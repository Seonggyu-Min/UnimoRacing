using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Data.Common;
using Unity.VisualScripting;
using System.Threading.Tasks;

namespace YTW
{
    public class AudioManager : Singleton<AudioManager>
    {
        // 상수 Resources 폴더 안에서의 경로
        //private const string AUDIO_DB_PATH = "Audio/AudioDB";
        //private const string AUDIO_MIXER_PATH = "Audio/GameAudioMixer";
        private const string AUDIO_DB_ADDRESS = "AudioDB"; // 예시 주소
        private const string AUDIO_MIXER_ADDRESS = "GameAudioMixer"; // 예시 주소

        [Header("오디오 데이터베이스")]
        [SerializeField] private AudioDB _audioDB;

        [Header("오디오 믹서")]
        [SerializeField] private AudioMixer _mixer;

        [Header("기본 오디오 소스")]
        [SerializeField] private AudioSource _bgmSource;

        [SerializeField] private bool initializeOnStart = true;
        // 내부 변수
        // 오디오 데이터를 이름으로 빠르게 찾기 위한 Dictionary
        private Dictionary<string, AudioData> _audioDataDict;
        // SFX 재생에 사용할 AudioSource들을 담아두는 리스트 (오브젝트 풀)
        private List<AudioSource> _sfxPool;
        // SFX 풀 중에서 현재 사용 가능(재생 중이 아닌)한 AudioSource들을 담아두는 큐
        private Queue<AudioSource> _availableSfxSources;
        // 재생이 끝난 SFX AudioSource를 자동으로 풀에 반납하는 코루틴들을 관리하는 딕셔너리
        private Dictionary<AudioSource, Coroutine> _activeReturnCoroutines;
        // BGM 페이드인/아웃 효과를 처리하는 코루틴을 저장하는 변수
        private Coroutine _bgmFadeCo;
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        public string CurrentBgmKey { get; private set; }
        // 상수
        // SFX 오브젝트 풀을 처음에 몇 개 만들어 둘지 정하는 상수
        private const int POOL_INITIAL_SIZE = 10;
        // SFX 오브젝트 풀이 최대로 늘어날 수 있는 크기를 제한하는 상수
        private const int POOL_MAX_SIZE = 64;
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        public event System.Action Initialized;

        #region 초기화
        protected override void Awake()
        {
            base.Awake();
        }

        private async void Start()
        {
            if (initializeOnStart)
                await InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            // ResourceManager가 생성될 때까지 대기
            while (Manager.Resource == null)
            {
                // Task.Yield() : 한 프레임 뒤로 미루고 다시 검사
                await Task.Yield();
            }

            //_audioDB = await Manager.Resource.LoadAsync<AudioDB>(AUDIO_DB_ADDRESS);
            //_mixer = await Manager.Resource.LoadAsync<AudioMixer>(AUDIO_MIXER_ADDRESS);
            if (_audioDB == null) _audioDB = Resources.Load<AudioDB>("Audio/AudioDB");
            if (_mixer == null) _mixer = Resources.Load<AudioMixer>("Audio/GameAudioMixer");

            if (_audioDB == null || _mixer == null)
            {
                Debug.LogError("[AudioManager] 필수 에셋 로드 실패");
                return;
            }

            // AudioDB 안에 등록된 모든 AudioData 항목을 미리 로드해서 딕셔너리에 저장
            await PreloadAudioClips();


            // SFX 풀링 초기화 (효율적인 사운드 재생을 위해 오디오 소스 미리 만들어둠)
            InitializeSfxPool();
            _activeReturnCoroutines = new Dictionary<AudioSource, Coroutine>();

            // BGM 전용 오디오 소스가 없으면 자동으로 생성
            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.playOnAwake = false;
            }

            // PlayerPrefs에서 이전 볼륨 세팅 불러오기
            LoadVolumeSettings();
            _isInitialized = true;
            Debug.Log("[AudioManager] 비동기 초기화 및 오디오 프리로딩 완료.");
            Initialized?.Invoke();
        }

        // AudioDB에 등록된 모든 오디오 데이터를 돌면서 해당 오디오 클립을 Addressables에서 미리 로드
        private async Task PreloadAudioClips()
        {
            _audioDataDict = new Dictionary<string, AudioData>(_audioDB.AudioDataList.Count, StringComparer.OrdinalIgnoreCase);

            var loadingTasks = new List<Task>();

            foreach (var data in _audioDB.AudioDataList)
            {
                // 오디오 데이터 하나마다 비동기 로드 작업 추가
                loadingTasks.Add(LoadClipForData(data));
            }

            // 모든 로드 작업이 완료될 때까지 비동기적으로 기다림
            await Task.WhenAll(loadingTasks);
        }

        // AudioData 하나를 실제로 로드하는 함수
        private async Task LoadClipForData(AudioData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.ClipAddress)) return;

            // ResourceManager를 통해 AudioClip을 로드
            var loadedClip = await Manager.Resource.LoadAsync<AudioClip>(data.ClipAddress);
            if (loadedClip != null)
            {
                data.Clip = loadedClip;
                // ClipName이 비어있지 않고 아직 등록되지 않았다면 딕셔너리에 추가
                if (!string.IsNullOrWhiteSpace(data.ClipName) && !_audioDataDict.ContainsKey(data.ClipName))
                {
                    _audioDataDict.Add(data.ClipName, data);
                }
            }
        }
        #endregion

        #region BGM 제어
        // BGM을 재생
        // name: 재생할 BGM의 이름, fadeTime: 전환 시간, forceRestart: 이미 같은 BGM이 나올 때 강제로 다시 재생할지 여부
        public void PlayBGM(string name, float fadeTime = 1.0f, bool forceRestart = false)
        {

            if (!IsInitialized) { Debug.LogWarning("AudioManager가 아직 준비되지 않아 BGM을 재생할 수 없습니다."); return; }

            if (!_audioDataDict.TryGetValue(name.Trim(), out var data) || data?.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM '{name}'을 찾을 수 없거나 클립이 비어있습니다.");
                return;
            }

            CurrentBgmKey = name;

            if (_bgmSource == null) return;
            if (!forceRestart && _bgmSource.isPlaying && _bgmSource.clip == data.Clip) return;
            if (_bgmFadeCo != null) StopCoroutine(_bgmFadeCo);
            _bgmFadeCo = StartCoroutine(IE_FadeToNewBGM(data, fadeTime));
        }

        // BGM 재생을 중지
        public void StopBGM(float fadeTime = 0.5f)
        {
            if (_bgmSource == null || !_bgmSource.isPlaying) return;
            if (_bgmFadeCo != null) StopCoroutine(_bgmFadeCo);
            _bgmFadeCo = StartCoroutine(IE_FadeOutBGM(fadeTime));
        }

        // 새로운 BGM으로 부드럽게 전환하는 코루틴
        private IEnumerator IE_FadeToNewBGM(AudioData data, float fadeTime)
        {
            if (_bgmSource.isPlaying)
            {
                // 전체 전환 시간의 절반만큼 사용하여 현재 BGM을 서서히 끔
                // yield return은 해당 코루틴(IE_FadeOutBGM)이 끝날 때까지 여기서 기다리라는 의미
                yield return IE_FadeOutBGM(fadeTime / 2);
            }
            
            // 새로운 BGM의 오디오 클립과 설정을 BGM 소스에 적용
            _bgmSource.clip = data.Clip;
            _bgmSource.loop = true;
            _bgmSource.outputAudioMixerGroup = data.MixerGroup;
            _bgmSource.pitch = data.Pitch;
            _bgmSource.Play();
            yield return IE_FadeInBGM(data.Volume, fadeTime / 2);
        }

        // BGM 소리를 서서히 줄여 끄는 코루틴
        private IEnumerator IE_FadeOutBGM(float fadeTime)
        {
            float startVolume = _bgmSource.volume;
            float timer = 0f;

            while (timer < fadeTime)
            {
                // Time.unscaledDeltaTime은 Time.timeScale의 영향을 받지 않는 실제 시간 변화량입니다. (일시정지 중에도 소리가 꺼지도록)
                timer += Time.unscaledDeltaTime;
                // 볼륨을 startVolume에서 0으로 부드럽게
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
                yield return null;
            }
            _bgmSource.volume = 0f;
            _bgmSource.Stop();
        }

        // BGM 소리를 0에서 목표 볼륨까지 서서히 키우는 코루틴
        private IEnumerator IE_FadeInBGM(float targetVolume, float fadeTime)
        {
            _bgmSource.volume = 0f;
            float timer = 0f;
            while (timer < fadeTime)
            {
                timer += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(0f, targetVolume, timer / fadeTime);
                yield return null;
            }
            _bgmSource.volume = targetVolume;
        }
        #endregion

        #region SFX 제어
        // 3D 위치에서 SFX를 재생
        public AudioSource PlaySFX(string name, Vector3 position)
        {
            return PlaySFXInternal(name, true, position);
        }

        // 2D 공간에서 SFX를 재생
        public AudioSource PlaySFX(string name)
        {
            if (!IsInitialized) { Debug.LogWarning("AudioManager가 아직 준비되지 않았습니다."); return null; }
            return PlaySFXInternal(name, false, Vector3.zero);
        }

        // SFX 재생 로직
        private AudioSource PlaySFXInternal(string name, bool is3D, Vector3 position)
        {
            if (!_audioDataDict.TryGetValue(name.Trim(), out var data) || data?.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX '{name}'을 찾을 수 없거나 클립이 비어있습니다.");
                return null;
            }
            AudioSource source = GetAvailableSfxSource();
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] 사용 가능한 SFX 소스가 없습니다. '{name}' 재생 실패.");
                return null;
            }
            // 가져온 AudioSource의 위치와 설정을 오디오 데이터에 맞게 구성
            source.transform.position = is3D ? position : transform.position;
            source.clip = data.Clip;
            source.volume = data.Volume;
            source.pitch = data.Pitch;
            source.loop = data.Loop;
            source.outputAudioMixerGroup = data.MixerGroup;
            source.spatialBlend = is3D ? 1.0f : 0.0f;
            source.Play();
            if (!data.Loop)
            {
                float duration = data.Clip.length / (data.Pitch <= 0 ? 1f : data.Pitch);
                var returnCo = StartCoroutine(IE_ReturnToPoolAfterPlay(source, duration));
                _activeReturnCoroutines[source] = returnCo;
            }
            return source;
        }

        // 반복 재생 중인 SFX를 멈추는 함수
        public void StopLoopedSFX(AudioSource sourceToStop)
        {
            if (sourceToStop != null && sourceToStop.isPlaying && sourceToStop.loop)
            {
                ReturnToPool(sourceToStop);
            }
        }
        #endregion

        #region 볼륨 제어
        // 게임 시작 시 PlayerPrefs에 저장된 볼륨 값을 불러오는 함수
        private void LoadVolumeSettings()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f));
            SetBGMVolume(PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f));
            SetSFXVolume(PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f));
        }

        // UI 슬라이더 등에서 호출
        public void SetMasterVolume(float volume) => SetVolume(MASTER_VOLUME_KEY, volume);
        public void SetBGMVolume(float volume) => SetVolume(BGM_VOLUME_KEY, volume);
        public void SetSFXVolume(float volume) => SetVolume(SFX_VOLUME_KEY, volume);

        // 볼륨 설정의 실제 로직을 처리
        private void SetVolume(string key, float volume)
        {
            if (_mixer == null)
            {
                Debug.LogError("[AudioManager] AudioMixer가 할당되지 않아 볼륨을 조절할 수 없습니다.");
                return;
            }
            volume = Mathf.Clamp01(volume);
            
            float db = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            // AudioMixer의 파라미터(Exposed Parameter) 값을 변경
            if (!_mixer.SetFloat(key, db))
            {
                Debug.LogWarning($"[AudioManager] Mixer에 '{key}' 파라미터가 없거나 설정에 실패했습니다.");
            }

            // 변경된 볼륨 값을 기기에 저장하여 다음 실행 시에도 유지
            PlayerPrefs.SetFloat(key, volume);
        }
        #endregion

        #region 오브젝트 풀링
        // SFX 재생에 사용할 AudioSource들을 미리 생성해두는 함수
        private void InitializeSfxPool()
        {
            // 리스트와 큐를 초기 용량에 맞게 생성
            _sfxPool = new List<AudioSource>(POOL_INITIAL_SIZE);
            _availableSfxSources = new Queue<AudioSource>(POOL_INITIAL_SIZE);
            for (int i = 0; i < POOL_INITIAL_SIZE; i++)
            {
                CreatePooledSfxSource(false);
            }
        }

        // 사용 가능한 AudioSource를 풀에서 가져오는 함수
        private AudioSource GetAvailableSfxSource()
        {
            if (_availableSfxSources.Count > 0)
            {
                AudioSource source = _availableSfxSources.Dequeue();
                source.gameObject.SetActive(true);
                return source;
            }
            if (_sfxPool.Count < POOL_MAX_SIZE)
            {
                return CreatePooledSfxSource(true);
            }
            Debug.LogWarning("[AudioManager] SFX 풀 최대치에 도달했습니다.");
            return null;
        }

        // 풀에 들어갈 새로운 AudioSource 게임 오브젝트를 생성하는 함수
        private AudioSource CreatePooledSfxSource(bool isActive)
        {
            var go = new GameObject($"SfxSource_Pooled_{_sfxPool.Count}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _sfxPool.Add(src);
            if (!isActive)
            {
                go.SetActive(false);
                _availableSfxSources.Enqueue(src);
            }
            return src;
        }

        // 사용이 끝난 AudioSource를 풀에 반납하는 함수
        private void ReturnToPool(AudioSource source)
        {
            if (source == null) return;
            if (_activeReturnCoroutines.TryGetValue(source, out Coroutine existingCo))
            {
                if (existingCo != null) StopCoroutine(existingCo);
                _activeReturnCoroutines.Remove(source);
            }
            source.Stop();
            source.clip = null;
            source.loop = false;
            if (source.gameObject.activeSelf)
            {
                source.gameObject.SetActive(false);
                _availableSfxSources.Enqueue(source);
            }
        }

        // 일정 시간(클립 길이)이 지난 후 소스를 풀에 반납하는 코루틴
        private IEnumerator IE_ReturnToPoolAfterPlay(AudioSource source, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            _activeReturnCoroutines.Remove(source);
            ReturnToPool(source);
        }

        // 애플리케이션이 종료될 때 호출되는 이벤트 함수
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            PlayerPrefs.Save();
        }
        #endregion

        #region 지속 사운드 제어

        // 지정된 AudioSource에서 루프 사운드를 재생 (주행음에 사용. 없다면 사용 x)
        // 오디오소스, db에 등록된 이름, 페이드타임 (서서히 켜지는 시간)
        public AudioData PlayLoopingSoundOn(AudioSource source, string name, float fadeTime = 0.1f)
        {
            if (source == null)
            {
                Debug.LogError("[AudioManager] 사운드를 재생할 AudioSource가 null입니다.");
                return null;
            }

            if (!_audioDataDict.TryGetValue(name.Trim(), out var data) || data?.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] 지속 사운드 '{name}'을 찾을 수 없거나 클립이 비어있습니다.");
                return null;
            }

            // 이미 같은 클립을 재생 중이면 아무것도 하지 않음
            if (source.isPlaying && source.clip == data.Clip) return data;

            // AudioSource에 필요한 설정
            source.clip = data.Clip;
            source.volume = 0; // 페이드 인을 위해 볼륨을 0에서 시작
            source.pitch = data.Pitch; // DB에 설정된 초기 피치 값을 적용
            source.loop = true; // 지속 사운드는 항상 루프되도록 설정
            source.outputAudioMixerGroup = data.MixerGroup;
            source.spatialBlend = 1.0f; // 주행음은 보통 3D 사운드이므로 입체감

            source.Play();

            // 페이드 인 효과를 주는 코루틴
            StartCoroutine(IE_FadeSource(source, data.Volume, fadeTime));

            return data;
        }


        // 지정된 AudioSource의 재생을 부드럽게 멈춥니다.
        // 멈출 오디오소스, 페이드 타입(서서히 멈추게)
        public void StopSoundOn(AudioSource source, float fadeTime = 0.2f)
        {
            if (source == null || !source.isPlaying) return;

            // 페이드 아웃 후 정지하는 코루틴
            StartCoroutine(IE_FadeSource(source, 0f, fadeTime, true));
        }

        // 지정된 AudioSource의 볼륨을 조절하는 범용 페이드 코루틴
        private IEnumerator IE_FadeSource(AudioSource source, float targetVolume, float duration, bool stopAfterFade = false)
        {
            // 코루틴이 시작될 때의 볼륨 값을 저장
            float startVolume = source.volume;
            float timer = 0f;

            while (timer < duration)
            {
                // 루프 중간에 AudioSource가 파괴될 경우를 대비해 null을 체크
                if (source == null) yield break; // 코루틴 즉시 중단

                timer += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null; // 다음 프레임까지 대기
            }

            // 루프가 끝난 후 최종 값으로 확실하게 설정
            if (source != null)
            {
                source.volume = targetVolume;
                // true가 전달된 경우, 페이드 아웃이 끝난 후 재생을 완전히 멈춤
                if (stopAfterFade)
                {
                    source.Stop();
                }
            }
        }

        #endregion

        #region 패치 후 재로딩
        // 패치 완료 후 DB 등록 AudioData의 AudioClip을 강제로 재로딩. 옵션으로 기존 BGM 재개
        public async Task ReloadAllAudioClipsAfterPatchAsync(bool replayCurrentBgm = true)
        {
            bool wasPlaying = _bgmSource != null && _bgmSource.isPlaying;
            string keepKey = CurrentBgmKey;

            var tasks = new List<Task>(_audioDataDict.Count);
            foreach (var kv in _audioDataDict)
            {
                var data = kv.Value;
                if (data == null || string.IsNullOrWhiteSpace(data.ClipAddress)) continue;
                tasks.Add(ReloadOne(data));
            }

            await Task.WhenAll(tasks);

            if (replayCurrentBgm && wasPlaying && !string.IsNullOrWhiteSpace(keepKey))
            {
                PlayBGM(keepKey, fadeTime: 0.25f, forceRestart: true);
            }
        }

        private async Task ReloadOne(AudioData data)
        {
            var newClip = await Manager.Resource.ForceReloadAsync<AudioClip>(data.ClipAddress);
            if (newClip != null)
            {
                data.Clip = newClip;
                Debug.Log($"[AudioManager] Reloaded: {data.ClipName} ({data.ClipAddress})");
            }
            else
            {
                Debug.LogWarning($"[AudioManager] Reload failed: {data.ClipName} ({data.ClipAddress})");
            }
        }

        public async Task ReloadAudioClipByNameAsync(string clipName)
        {
            if (!_audioDataDict.TryGetValue(clipName.Trim(), out var data) || data == null) return;


            var newClip = await Manager.Resource.ForceReloadAsync<AudioClip>(data.ClipAddress);
            if (newClip != null)
            {
                data.Clip = newClip;

                // 현재 재생 중 BGM이면 즉시 반영
                if (CurrentBgmKey?.Equals(clipName, StringComparison.OrdinalIgnoreCase) == true && _bgmSource != null)
                {
                    _bgmSource.clip = newClip;
                    _bgmSource.Play();
                }
            }
        }
        #endregion
    }

}
