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
        [Tooltip("�ӵ�(0~1)�� ���� ������ Pitch ��ȭ �")]
        [SerializeField] private AnimationCurve speedToPitchCurve = AnimationCurve.Linear(0f, 0.8f, 1f, 2.0f);
        [Tooltip("�ӵ�(0~1)�� ���� ������ Volume ��ȭ �")]
        [SerializeField] private AnimationCurve speedToVolumeCurve = AnimationCurve.Linear(0f, 0.5f, 1f, 1f);
        [Tooltip("���尡 ��ǥ ������ ���ϴ� �ε巯���� ����")]
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

        private void Start()
        {

            if (_engineAudioSource == null)
            {
                _engineAudioSource = GetComponent<AudioSource>();
            }

            _currentSpeed = _maxSpeed;

            // ������ ���۵Ǹ� ������("EngineSound")�� �ݺ� ����մϴ�.
            if (_engineAudioSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLoopingSoundOn(_engineAudioSource, "EngineSound");
            }
        }


        private void Update()
        {
            if (!_isCrashed && !_isFinished)
            {
                HandleMovement();
            }

            UpdateEngineSound();

            // �浹 �׽�Ʈ ���� '1'Ű
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                // �̹� �浹 �� ȸ�� ���̶�� �ߺ� ���� ����
                if (_crashRecoveryCo != null)
                {
                    StopCoroutine(_crashRecoveryCo);
                }
                // �浹 �� ȸ�� �ڷ�ƾ ����
                _crashRecoveryCo = StartCoroutine(CrashAndRecoverRoutine());
            }
        }

        private void UpdateEngineSound()
        {
            if (_engineAudioSource == null || !_engineAudioSource.isPlaying) return;

            // ���� �ӵ��� 0�� 1 ������ ����(����ȭ ��)�� ����մϴ�.
            float normalizedSpeed = Mathf.Clamp01(_currentSpeed / _maxSpeed);

            // AnimationCurve���� ���� �ӵ� ������ �ش��ϴ� ��ǥ Pitch�� Volume ���� �����ɴϴ�.
            float targetPitch = speedToPitchCurve.Evaluate(normalizedSpeed);
            float targetVolume = speedToVolumeCurve.Evaluate(normalizedSpeed);

            // Lerp�� ����Ͽ� ���� ������ ��ǥ ������ �ε巴�� ��ȭ��ŵ�ϴ�.
            _engineAudioSource.pitch = Mathf.Lerp(_engineAudioSource.pitch, targetPitch, Time.deltaTime * soundSmoothSpeed);
            _engineAudioSource.volume = Mathf.Lerp(_engineAudioSource.volume, targetVolume, Time.deltaTime * soundSmoothSpeed);
        }

        private IEnumerator CrashAndRecoverRoutine()
        {
            // 1. �浹 ����
            _isCrashed = true;
            _currentSpeed = 0f; // �ӵ��� ��� 0���� ����

            if (AudioManager.Instance != null)
            {
                Debug.Log("�浹. ������ ���� �� �浹�� ���.");
                // ������ ���� (���)
                AudioManager.Instance.StopSoundOn(_engineAudioSource, 0.0f);
                // �浹�� ���
                AudioManager.Instance.PlaySFX("CollisionSound", transform.position);
            }

            // 2. 2�� ���
            yield return new WaitForSeconds(2f);

            // 3. ȸ�� ����
            Debug.Log("2�� ��, ȸ�� ����");
            if (AudioManager.Instance != null)
            {
                // ������ �ٽ� ���
                AudioManager.Instance.PlayLoopingSoundOn(_engineAudioSource, "EngineSound");
            }

            // ������ �ִ� �ӵ����� ���� (��: 1.5�� ����)
            float recoveryTime = 5f;
            float timer = 0f;
            while (timer < recoveryTime)
            {
                timer += Time.deltaTime;
                // �ð��� ���� �ӵ��� 0���� _maxSpeed���� �ε巴�� ����
                _currentSpeed = Mathf.Lerp(0f, _maxSpeed, timer / recoveryTime);
                yield return null; // ���� �����ӱ��� ���
            }

            // 4. ȸ�� �Ϸ�
            _currentSpeed = _maxSpeed; // �ӵ��� �ִ�ġ�� Ȯ���ϰ� ����
            _isCrashed = false; // �浹 ���� ����
            _crashRecoveryCo = null; // �ڷ�ƾ ���� ����
            Debug.Log("�浹 ȸ��");
        }
        private void HandleMovement()
        {
            // 1. ����� ���
            float trackLength = _trackSpline.GetComponent<SplineContainer>().CalculateLength();
            _progress += (_maxSpeed / trackLength) * Time.deltaTime;

            if (_progress >= 1f && !_isFinished)
            {
                _isFinished = true; // ���� ���·� ���� (�ߺ� ���� ����)
                _progress = 1f;

                Debug.Log("����! TestScene1���� ���ư��ϴ�.");

                // 1. ���� ���带 1�ʿ� ���� �ε巴�� ����ϴ�.
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.StopSoundOn(_engineAudioSource, 1.0f);
                }

                // 2. SceneManager�� ���� TestScene1�� �ε��մϴ�.
                if (Manager.Scene != null)
                {
                    Manager.Scene.LoadScene(SceneType.YTW_TestScene1);
                }

                return; // ���������Ƿ� �Ʒ� �̵� ������ �������� �ʽ��ϴ�.
            }

            // 2. ��ǥ ��ġ ���
            var (targetPosition, targetForward, targetUp) = _trackSpline.GetLanePoint(_progress, _currentLaneIndex);

            // 3. �ε巯�� ��ġ �̵� �� �ڼ� ����
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * _laneChangeSpeed);
            transform.rotation = Quaternion.LookRotation(targetForward, targetUp);
        }

        public void MoveLeft()
        {
            if (_currentLaneIndex > MIN_LANE_INDEX)
            {
                _currentLaneIndex--;
            }
        }

        public void MoveRight()
        {
            if (_currentLaneIndex < MAX_LANE_INDEX)
            {
                _currentLaneIndex++;
            }
        }

    }
}

