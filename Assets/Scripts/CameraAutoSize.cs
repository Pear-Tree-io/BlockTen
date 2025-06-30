using UnityEngine;

public class CameraAutoSize : MonoBehaviour
{
	[SerializeField] private Camera _camera;
	[SerializeField] private float _baseOrthographicSize = 10f;
	[SerializeField] private float _baseAspect =  9f / 16f;

	private void Start()
	{
		AdjustCameraSize();
	}

	private void AdjustCameraSize()
	{
		var currentAspect = (float)Screen.width / Screen.height;

		if (currentAspect < _baseAspect)
		{
			var scale = _baseAspect / currentAspect;
			_camera.orthographicSize = _baseOrthographicSize * scale;
		}
		else
		{
			_camera.orthographicSize = _baseOrthographicSize;
		}
	}
}