using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

namespace YTW
{
    public class PlayerController_Test_YTW : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource _engineAudioSource;

        [Header("Engine Sound Mapping")]
        [Tooltip("속도(0~1)에 따른 엔진음 Pitch 변화 곡선")]
        [SerializeField] private AnimationCurve speedToPitchCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 2.0f);
        [Tooltip("속도(0~1)에 따른 엔진음 Volume 변화 곡선")]
        [SerializeField] private AnimationCurve speedToVolumeCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
        [Tooltip("사운드가 목표 값으로 변하는 부드러움의 정도")]
        [SerializeField] private float soundSmoothSpeed = 8f;

        [Header("Track Info")]
        [SerializeField] private TrackSpline_Test _trackSpline;

        [Header("Player Stats")]
        [SerializeField] private float _maxSpeed = 10f;
        [SerializeField] private float _laneChangeSpeed = 15f;

        private float _currentSpeed;
        private int _currentLaneIndex = 1;
        private bool _isCrashed = false;
        private bool _isFinished = false;
        private Coroutine _crashRecoveryCo;

        private const int MAX_LANE_INDEX = 3;
        private const int MIN_LANE_INDEX = 0;

        private float _progress = 0f;
        private float _baseEngineVolume = 1.0f;

        private void Start()
        {

            if (_engineAudioSource == null)
            {
                _engineAudioSource = GetComponent<AudioSource>();
            }

            _currentSpeed = _maxSpeed;

            StartEngineSound();
        }


        private void Update()
        {
            if (!_isCrashed && !_isFinished)
            {
                HandleMovement();
            }

            UpdateEngineSound();

            // 충돌 테스트 숫자 '1'키
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                // 이미 충돌 후 회복 중이라면 중복 실행 방지
                if (_crashRecoveryCo != null)
                {
                    StopCoroutine(_crashRecoveryCo);
                }
                // 충돌 및 회복 코루틴 시작
                _crashRecoveryCo = StartCoroutine(CrashAndRecoverRoutine());
            }
        }

        private void UpdateEngineSound()
        {
            if (_engineAudioSource == null || !_engineAudioSource.isPlaying) return;

            // 현재 속도를 0과 1 사이의 비율(정규화 값)로 계산합니다.
            float normalizedSpeed = Mathf.Clamp01(_currentSpeed / _maxSpeed);

            // AnimationCurve에서 현재 속도 비율에 해당하는 목표 Pitch와 Volume 값을 가져옵니다.
            float relativeVolume = speedToVolumeCurve.Evaluate(normalizedSpeed);

            float targetVolume = _baseEngineVolume * relativeVolume;

            // Lerp를 사용하여 현재 값에서 목표 값으로 부드럽게 변화시킵니다.
            _engineAudioSource.volume = Mathf.Lerp(_engineAudioSource.volume, targetVolume, Time.deltaTime * soundSmoothSpeed);
            _engineAudioSource.pitch = Mathf.Lerp(_engineAudioSource.pitch, speedToPitchCurve.Evaluate(normalizedSpeed), Time.deltaTime * soundSmoothSpeed);
        }

        private IEnumerator CrashAndRecoverRoutine()
        {
            // 1. 충돌 시작: 멈추고 소리를 끈다
            _isCrashed = true;
            _currentSpeed = 0f;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopSoundOn(_engineAudioSource, 0.0f);
                AudioManager.Instance.PlaySFX("CollisionSound", transform.position);
            }

            // 2. 가만히 기다린다
            yield return new WaitForSeconds(2f);

            // 3. 회복 시작: 이제 움직일 수 있도록 플래그를 해제하고 엔진음을 다시 켠다
            _isCrashed = false;
            StartEngineSound();

            // 4. 움직이면서 서서히 가속한다
            float recoveryTime = 5f;
            float timer = 0f;
            while (timer < recoveryTime)
            {
                // _isCrashed가 false이므로 Update에서 HandleMovement가 호출되어 차가 움직인다
                timer += Time.deltaTime;
                _currentSpeed = Mathf.Lerp(0f, _maxSpeed, timer / recoveryTime);
                yield return null;
            }

            // 5. 회복 완료
            _currentSpeed = _maxSpeed;
            _crashRecoveryCo = null;
        }

        private void StartEngineSound()
        {
            if (_engineAudioSource != null && AudioManager.Instance != null)
            {
                var engineData = AudioManager.Instance.PlayLoopingSoundOn(_engineAudioSource, "EngineSound");
                if (engineData != null)
                {
                    // AudioData에 설정된 Volume 값을 기본 볼륨으로 저장
                    _baseEngineVolume = engineData.Volume;
                }
            }
        }

        private void HandleMovement()
        {
            // 1. 진행률 계산
            float trackLength = _trackSpline.GetComponent<SplineContainer>().CalculateLength();
            _progress += (_currentSpeed / trackLength) * Time.deltaTime;

            if (_progress >= 1f && !_isFinished)
            {
                _isFinished = true; // 완주 상태로 변경 (중복 실행 방지)
                _progress = 1f;

                Debug.Log("완주! TestScene1으로 돌아갑니다.");

                // 1. 엔진 사운드를 1초에 걸쳐 부드럽게 멈춥니다.
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopSoundOn(_engineAudioSource, 1.0f);
                }

                // 2. SceneManager를 통해 TestScene1을 로드합니다.
                if (Manager.Scene != null)
                {
                    Manager.Scene.LoadScene(SceneType.YTW_TestScene1);
                }

                return; // 완주했으므로 아래 이동 로직을 실행하지 않습니다.
            }

            // 2. 목표 위치 계산
            var (targetPosition, targetForward, targetUp) = _trackSpline.GetLanePoint(_progress, _currentLaneIndex);

            // 3. 부드러운 위치 이동 및 자세 갱신
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * _laneChangeSpeed);
            transform.rotation = Quaternion.LookRotation(targetForward, targetUp);
        }

        public void MoveLeft()
        {
            Debug.Log("왼쪽 이동");
            if (_currentLaneIndex > MIN_LANE_INDEX)
            {
                _currentLaneIndex--;
                Manager.Audio.PlaySFX("LineChangeSFX");
            }
        }

        public void MoveRight()
        {
            Debug.Log("오른쪽 이동");
            if (_currentLaneIndex < MAX_LANE_INDEX)
            {
                _currentLaneIndex++;
                Manager.Audio.PlaySFX("LineChangeSFX");
            }
        }

    }
}

