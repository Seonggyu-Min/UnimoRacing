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
        // ��� Resources ���� �ȿ����� ���
        //private const string AUDIO_DB_PATH = "Audio/AudioDB";
        //private const string AUDIO_MIXER_PATH = "Audio/GameAudioMixer";
        private const string AUDIO_DB_ADDRESS = "AudioDB"; // ���� �ּ�
        private const string AUDIO_MIXER_ADDRESS = "GameAudioMixer"; // ���� �ּ�

        [Header("����� �����ͺ��̽�")]
        [SerializeField] private AudioDB _audioDB;

        [Header("����� �ͼ�")]
        [SerializeField] private AudioMixer _mixer;

        [Header("�⺻ ����� �ҽ�")]
        [SerializeField] private AudioSource _bgmSource;

        [SerializeField] private bool initializeOnStart = true;
        // ���� ����
        // ����� �����͸� �̸����� ������ ã�� ���� Dictionary
        private Dictionary<string, AudioData> _audioDataDict;
        // SFX ����� ����� AudioSource���� ��Ƶδ� ����Ʈ (������Ʈ Ǯ)
        private List<AudioSource> _sfxPool;
        // SFX Ǯ �߿��� ���� ��� ����(��� ���� �ƴ�)�� AudioSource���� ��Ƶδ� ť
        private Queue<AudioSource> _availableSfxSources;
        // ����� ���� SFX AudioSource�� �ڵ����� Ǯ�� �ݳ��ϴ� �ڷ�ƾ���� �����ϴ� ��ųʸ�
        private Dictionary<AudioSource, Coroutine> _activeReturnCoroutines;
        // BGM ���̵���/�ƿ� ȿ���� ó���ϴ� �ڷ�ƾ�� �����ϴ� ����
        private Coroutine _bgmFadeCo;
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        public string CurrentBgmKey { get; private set; }
        // ���
        // SFX ������Ʈ Ǯ�� ó���� �� �� ����� ���� ���ϴ� ���
        private const int POOL_INITIAL_SIZE = 10;
        // SFX ������Ʈ Ǯ�� �ִ�� �þ �� �ִ� ũ�⸦ �����ϴ� ���
        private const int POOL_MAX_SIZE = 64;
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        public event System.Action Initialized;

        #region �ʱ�ȭ
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

            // ResourceManager�� ������ ������ ���
            while (Manager.Resource == null)
            {
                // Task.Yield() : �� ������ �ڷ� �̷�� �ٽ� �˻�
                await Task.Yield();
            }

            //_audioDB = await Manager.Resource.LoadAsync<AudioDB>(AUDIO_DB_ADDRESS);
            //_mixer = await Manager.Resource.LoadAsync<AudioMixer>(AUDIO_MIXER_ADDRESS);
            if (_audioDB == null) _audioDB = Resources.Load<AudioDB>("Audio/AudioDB");
            if (_mixer == null) _mixer = Resources.Load<AudioMixer>("Audio/GameAudioMixer");

            if (_audioDB == null || _mixer == null)
            {
                Debug.LogError("[AudioManager] �ʼ� ���� �ε� ����");
                return;
            }

            // AudioDB �ȿ� ��ϵ� ��� AudioData �׸��� �̸� �ε��ؼ� ��ųʸ��� ����
            await PreloadAudioClips();


            // SFX Ǯ�� �ʱ�ȭ (ȿ������ ���� ����� ���� ����� �ҽ� �̸� ������)
            InitializeSfxPool();
            _activeReturnCoroutines = new Dictionary<AudioSource, Coroutine>();

            // BGM ���� ����� �ҽ��� ������ �ڵ����� ����
            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.playOnAwake = false;
            }

            // PlayerPrefs���� ���� ���� ���� �ҷ�����
            LoadVolumeSettings();
            _isInitialized = true;
            Debug.Log("[AudioManager] �񵿱� �ʱ�ȭ �� ����� �����ε� �Ϸ�.");
            Initialized?.Invoke();
        }

        // AudioDB�� ��ϵ� ��� ����� �����͸� ���鼭 �ش� ����� Ŭ���� Addressables���� �̸� �ε�
        private async Task PreloadAudioClips()
        {
            _audioDataDict = new Dictionary<string, AudioData>(_audioDB.AudioDataList.Count, StringComparer.OrdinalIgnoreCase);

            var loadingTasks = new List<Task>();

            foreach (var data in _audioDB.AudioDataList)
            {
                // ����� ������ �ϳ����� �񵿱� �ε� �۾� �߰�
                loadingTasks.Add(LoadClipForData(data));
            }

            // ��� �ε� �۾��� �Ϸ�� ������ �񵿱������� ��ٸ�
            await Task.WhenAll(loadingTasks);
        }

        // AudioData �ϳ��� ������ �ε��ϴ� �Լ�
        private async Task LoadClipForData(AudioData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.ClipAddress)) return;

            // ResourceManager�� ���� AudioClip�� �ε�
            var loadedClip = await Manager.Resource.LoadAsync<AudioClip>(data.ClipAddress);
            if (loadedClip != null)
            {
                data.Clip = loadedClip;
                // ClipName�� ������� �ʰ� ���� ��ϵ��� �ʾҴٸ� ��ųʸ��� �߰�
                if (!string.IsNullOrWhiteSpace(data.ClipName) && !_audioDataDict.ContainsKey(data.ClipName))
                {
                    _audioDataDict.Add(data.ClipName, data);
                }
            }
        }
        #endregion

        #region BGM ����
        // BGM�� ���
        // name: ����� BGM�� �̸�, fadeTime: ��ȯ �ð�, forceRestart: �̹� ���� BGM�� ���� �� ������ �ٽ� ������� ����
        public void PlayBGM(string name, float fadeTime = 1.0f, bool forceRestart = false)
        {

            if (!IsInitialized) { Debug.LogWarning("AudioManager�� ���� �غ���� �ʾ� BGM�� ����� �� �����ϴ�."); return; }

            if (!_audioDataDict.TryGetValue(name.Trim(), out var data) || data?.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM '{name}'�� ã�� �� ���ų� Ŭ���� ����ֽ��ϴ�.");
                return;
            }

            CurrentBgmKey = name;

            if (_bgmSource == null) return;
            if (!forceRestart && _bgmSource.isPlaying && _bgmSource.clip == data.Clip) return;
            if (_bgmFadeCo != null) StopCoroutine(_bgmFadeCo);
            _bgmFadeCo = StartCoroutine(IE_FadeToNewBGM(data, fadeTime));
        }

        // BGM ����� ����
        public void StopBGM(float fadeTime = 0.5f)
        {
            if (_bgmSource == null || !_bgmSource.isPlaying) return;
            if (_bgmFadeCo != null) StopCoroutine(_bgmFadeCo);
            _bgmFadeCo = StartCoroutine(IE_FadeOutBGM(fadeTime));
        }

        // ���ο� BGM���� �ε巴�� ��ȯ�ϴ� �ڷ�ƾ
        private IEnumerator IE_FadeToNewBGM(AudioData data, float fadeTime)
        {
            if (_bgmSource.isPlaying)
            {
                // ��ü ��ȯ �ð��� ���ݸ�ŭ ����Ͽ� ���� BGM�� ������ ��
                // yield return�� �ش� �ڷ�ƾ(IE_FadeOutBGM)�� ���� ������ ���⼭ ��ٸ���� �ǹ�
                yield return IE_FadeOutBGM(fadeTime / 2);
            }
            
            // ���ο� BGM�� ����� Ŭ���� ������ BGM �ҽ��� ����
            _bgmSource.clip = data.Clip;
            _bgmSource.loop = true;
            _bgmSource.outputAudioMixerGroup = data.MixerGroup;
            _bgmSource.pitch = data.Pitch;
            _bgmSource.Play();
            yield return IE_FadeInBGM(data.Volume, fadeTime / 2);
        }

        // BGM �Ҹ��� ������ �ٿ� ���� �ڷ�ƾ
        private IEnumerator IE_FadeOutBGM(float fadeTime)
        {
            float startVolume = _bgmSource.volume;
            float timer = 0f;

            while (timer < fadeTime)
            {
                // Time.unscaledDeltaTime�� Time.timeScale�� ������ ���� �ʴ� ���� �ð� ��ȭ���Դϴ�. (�Ͻ����� �߿��� �Ҹ��� ��������)
                timer += Time.unscaledDeltaTime;
                // ������ startVolume���� 0���� �ε巴��
                _bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
                yield return null;
            }
            _bgmSource.volume = 0f;
            _bgmSource.Stop();
        }

        // BGM �Ҹ��� 0���� ��ǥ �������� ������ Ű��� �ڷ�ƾ
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

        #region SFX ����
        // 3D ��ġ���� SFX�� ���
        public AudioSource PlaySFX(string name, Vector3 position)
        {
            return PlaySFXInternal(name, true, position);
        }

        // 2D �������� SFX�� ���
        public AudioSource PlaySFX(string name)
        {
            if (!IsInitialized) { Debug.LogWarning("AudioManager�� ���� �غ���� �ʾҽ��ϴ�."); return null; }
            return PlaySFXInternal(name, false, Vector3.zero);
        }

        // SFX ��� ����
        private AudioSource PlaySFXInternal(string name, bool is3D, Vector3 position)
        {
            if (!_audioDataDict.TryGetValue(name.Trim(), out var data) || data?.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] SFX '{name}'�� ã�� �� ���ų� Ŭ���� ����ֽ��ϴ�.");
                return null;
            }
            AudioSource source = GetAvailableSfxSource();
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] ��� ������ SFX �ҽ��� �����ϴ�. '{name}' ��� ����.");
                return null;
            }
            // ������ AudioSource�� ��ġ�� ������ ����� �����Ϳ� �°� ����
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

        // �ݺ� ��� ���� SFX�� ���ߴ� �Լ�
        public void StopLoopedSFX(AudioSource sourceToStop)
        {
            if (sourceToStop != null && sourceToStop.isPlaying && sourceToStop.loop)
            {
                ReturnToPool(sourceToStop);
            }
        }
        #endregion

        #region ���� ����
        // ���� ���� �� PlayerPrefs�� ����� ���� ���� �ҷ����� �Լ�
        private void LoadVolumeSettings()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f));
            SetBGMVolume(PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f));
            SetSFXVolume(PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f));
        }

        // UI �����̴� ��� ȣ��
        public void SetMasterVolume(float volume) => SetVolume(MASTER_VOLUME_KEY, volume);
        public void SetBGMVolume(float volume) => SetVolume(BGM_VOLUME_KEY, volume);
        public void SetSFXVolume(float volume) => SetVolume(SFX_VOLUME_KEY, volume);

        // ���� ������ ���� ������ ó��
        private void SetVolume(string key, float volume)
        {
            if (_mixer == null)
            {
                Debug.LogError("[AudioManager] AudioMixer�� �Ҵ���� �ʾ� ������ ������ �� �����ϴ�.");
                return;
            }
            volume = Mathf.Clamp01(volume);
            
            float db = volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
            // AudioMixer�� �Ķ����(Exposed Parameter) ���� ����
            if (!_mixer.SetFloat(key, db))
            {
                Debug.LogWarning($"[AudioManager] Mixer�� '{key}' �Ķ���Ͱ� ���ų� ������ �����߽��ϴ�.");
            }

            // ����� ���� ���� ��⿡ �����Ͽ� ���� ���� �ÿ��� ����
            PlayerPrefs.SetFloat(key, volume);
        }
        #endregion

        #region ������Ʈ Ǯ��
        // SFX ����� ����� AudioSource���� �̸� �����صδ� �Լ�
        private void InitializeSfxPool()
        {
            // ����Ʈ�� ť�� �ʱ� �뷮�� �°� ����
            _sfxPool = new List<AudioSource>(POOL_INITIAL_SIZE);
            _availableSfxSources = new Queue<AudioSource>(POOL_INITIAL_SIZE);
            for (int i = 0; i < POOL_INITIAL_SIZE; i++)
            {
                CreatePooledSfxSource(false);
            }
        }

        // ��� ������ AudioSource�� Ǯ���� �������� �Լ�
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
            Debug.LogWarning("[AudioManager] SFX Ǯ �ִ�ġ�� �����߽��ϴ�.");
            return null;
        }

        // Ǯ�� �� ���ο� AudioSource ���� ������Ʈ�� �����ϴ� �Լ�
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

        // ����� ���� AudioSource�� Ǯ�� �ݳ��ϴ� �Լ�
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

        // ���� �ð�(Ŭ�� ����)�� ���� �� �ҽ��� Ǯ�� �ݳ��ϴ� �ڷ�ƾ
        private IEnumerator IE_ReturnToPoolAfterPlay(AudioSource source, float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            _activeReturnCoroutines.Remove(source);
            ReturnToPool(source);
        }

        // ���ø����̼��� ����� �� ȣ��Ǵ� �̺�Ʈ �Լ�
        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            PlayerPrefs.Save();
        }
        #endregion

        #region ���� ���� ����

        // ������ AudioSource���� ���� ���带 ��� (�������� ���. ���ٸ� ��� x)
        // ������ҽ�, db�� ��ϵ� �̸�, ���̵�Ÿ�� (������ ������ �ð�)
        public AudioData PlayLoopingSoundOn(AudioSource source, string name, float fadeTime = 0.1f)
        {
            if (source == null)
            {
                Debug.LogError("[AudioManager] ���带 ����� AudioSource�� null�Դϴ�.");
                return null;
            }

            if (!_audioDataDict.TryGetValue(name.Trim(), out var data) || data?.Clip == null)
            {
                Debug.LogWarning($"[AudioManager] ���� ���� '{name}'�� ã�� �� ���ų� Ŭ���� ����ֽ��ϴ�.");
                return null;
            }

            // �̹� ���� Ŭ���� ��� ���̸� �ƹ��͵� ���� ����
            if (source.isPlaying && source.clip == data.Clip) return data;

            // AudioSource�� �ʿ��� ����
            source.clip = data.Clip;
            source.volume = 0; // ���̵� ���� ���� ������ 0���� ����
            source.pitch = data.Pitch; // DB�� ������ �ʱ� ��ġ ���� ����
            source.loop = true; // ���� ����� �׻� �����ǵ��� ����
            source.outputAudioMixerGroup = data.MixerGroup;
            source.spatialBlend = 1.0f; // �������� ���� 3D �����̹Ƿ� ��ü��

            source.Play();

            // ���̵� �� ȿ���� �ִ� �ڷ�ƾ
            StartCoroutine(IE_FadeSource(source, data.Volume, fadeTime));

            return data;
        }


        // ������ AudioSource�� ����� �ε巴�� ����ϴ�.
        // ���� ������ҽ�, ���̵� Ÿ��(������ ���߰�)
        public void StopSoundOn(AudioSource source, float fadeTime = 0.2f)
        {
            if (source == null || !source.isPlaying) return;

            // ���̵� �ƿ� �� �����ϴ� �ڷ�ƾ
            StartCoroutine(IE_FadeSource(source, 0f, fadeTime, true));
        }

        // ������ AudioSource�� ������ �����ϴ� ���� ���̵� �ڷ�ƾ
        private IEnumerator IE_FadeSource(AudioSource source, float targetVolume, float duration, bool stopAfterFade = false)
        {
            // �ڷ�ƾ�� ���۵� ���� ���� ���� ����
            float startVolume = source.volume;
            float timer = 0f;

            while (timer < duration)
            {
                // ���� �߰��� AudioSource�� �ı��� ��츦 ����� null�� üũ
                if (source == null) yield break; // �ڷ�ƾ ��� �ߴ�

                timer += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null; // ���� �����ӱ��� ���
            }

            // ������ ���� �� ���� ������ Ȯ���ϰ� ����
            if (source != null)
            {
                source.volume = targetVolume;
                // true�� ���޵� ���, ���̵� �ƿ��� ���� �� ����� ������ ����
                if (stopAfterFade)
                {
                    source.Stop();
                }
            }
        }

        #endregion

        #region ��ġ �� ��ε�
        // ��ġ �Ϸ� �� DB ��� AudioData�� AudioClip�� ������ ��ε�. �ɼ����� ���� BGM �簳
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

                // ���� ��� �� BGM�̸� ��� �ݿ�
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
