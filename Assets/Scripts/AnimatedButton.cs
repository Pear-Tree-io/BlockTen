using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnimatedButton : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler
{
	[SerializeField]
	private bool _interactable = true;
	public bool transition = true;
	public bool isHold = false;
	public float holdStartDelay = 0.3f;
	public float holdNextClickDelay = 0.08f;

	public bool interactable
	{
		get => _interactable;
		set
		{
			_interactable = value;
			InteractableChanged(_interactable);

			if (_interactable == false)
			{
				ResetTransition();
			}
		}
	}

	public Transform traTarget;
	public Image targetGraphic;
	public Sprite spriteEnabled;
	public Sprite spriteDisabled;
	public TextMeshProUGUI targetText;
	public Color colorEnabledText = Color.white;
	public Color colorDisabledText = Color.white;
	public GameObject objEnabled;
	private bool _isObjEnabled;
	public GameObject objDisabled;
	private bool _isObjDisabled;
	public GameObject[] objBadges;

	public void SetBadges(bool value)
	{
		foreach (var objBadge in objBadges)
		{
			objBadge.SetActive(value);
		}
	}

	private UnityEvent pointerUpAction => interactable ? _pointerUpAction : _pointerUpDisabledAction;
	[SerializeField]
	private UnityEvent _pointerUpAction;
	private UnityEvent _pointerUpDisabledAction;
	private Vector3 _originScale;
	private bool _isSpriteChangeable;
	private bool _isTextColorChangeable;

	protected override void Start()
	{
		base.Start();
		_isSpriteChangeable = spriteEnabled != null && spriteDisabled != null;
		_isTextColorChangeable = targetText != null;
		_isObjEnabled = objEnabled != null;
		_isObjDisabled = objDisabled != null;
		if (traTarget == null)
			traTarget = transform;
		_originScale = traTarget.localScale == Vector3.zero ? Vector3.one : traTarget.localScale;
		InteractableChanged(interactable);
	}

	private double _nextClickAt;
	private bool _isHolded;
	private Tweener _tweener = null;

	private void Update()
	{
		if (_pointerDown && isHold && _nextClickAt <= Time.timeAsDouble)
		{
			_isHolded = true;
			_nextClickAt = Time.timeAsDouble + holdNextClickDelay;
			pointerUpAction?.Invoke();

			_tweener ??= traTarget.DOScale(_originScale, holdNextClickDelay / 2).SetLoops(-1, LoopType.Yoyo);
		}
	}

	private void InteractableChanged(bool value)
	{
		if (_isSpriteChangeable)
		{
			targetGraphic.sprite = value ? spriteEnabled : spriteDisabled;
		}

		if (_isObjEnabled)
			objEnabled.SetActive(value);

		if (_isObjDisabled)
			objDisabled.SetActive(value == false);

		if (_isTextColorChangeable)
		{
			targetText.color = value ? colorEnabledText : colorDisabledText;
		}
	}

	private void ResetTransition()
	{
		if (_pointerDown == false)
			return;

		_pointerDown = false;

		if (transition == false)
			return;

		_tweener = null;
		DOTween.Kill(traTarget);
		traTarget.DOScale(_originScale, .1f);
	}

	public void SetOnClick(UnityEvent action)
	{
		_pointerUpAction = action;
	}

	public void SetOnClickAsync(UnityEvent action, UnityEvent callback)
	{
		interactable = false;
		_pointerUpAction = action;
	}

	public void SetOnDisabledClick(UnityEvent action)
	{
		_pointerUpDisabledAction = action;
	}

	private bool _isHoldBlock;

	public void SetHoldBlock()
	{
		if (isHold == false || _pointerDown == false)
			return;

		_isHoldBlock = true;
		isHold = false;

		ResetTransition();
	}

	private bool _pointerDown;

	public void OnPointerDown(PointerEventData eventData)
	{
		if (interactable == false)
			return;

		_pointerDown = true;
		_isHolded = false;

		if (isHold)
			_nextClickAt = Time.timeAsDouble + holdStartDelay;

		if (transition == false)
			return;

		DOTween.Kill(traTarget);
		traTarget.DOScale(_originScale * 0.9f, .1f);
	}

	public void OnPointerUp(PointerEventData eventData) //nullable
	{
		ResetTransition();

		if (_isHoldBlock)
			isHold = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		ResetTransition();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (interactable == false)
		{
			pointerUpAction?.Invoke();
			return;
		}

		if (_isHolded)
			return;
		
		AudioManager.Instance.PlaySFX(SFXType.Button);
		pointerUpAction?.Invoke();
	}

#if UNITY_EDITOR

	protected override void Reset()
	{
		targetGraphic = GetComponent<Image>();
	}
#endif
}