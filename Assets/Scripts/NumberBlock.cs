using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class NumberBlock : MonoBehaviour
{
	// --- Composite‐chain links (used by DraggableCompositeBlock) ---
	public NumberBlock neighborUp;
	public NumberBlock neighborDown;
	public NumberBlock neighborLeft;
	public NumberBlock neighborRight;

	// --- Value and display ---
	private int _value;
	public int Value
	{
		get => _value;
		set
		{
			_value = value;
			valueText.text = Value.ToString();
		}
	}
	[SerializeField]
	private TextMeshPro valueText;
	public SpriteRenderer spriteRenderer;

	// --- Preview animation ---
	[SerializeField]
	private Color previewColor = Color.yellow;
	private readonly Coroutine _previewRoutine;
	private Color _originalColor;

	/// <summary>
	/// Assigns a random value between 1 and 8 using a 55/45 split,
	/// then updates the label.
	/// </summary>
	public void AssignRandom()
	{
		//Value = Random.Range(1, 10);

		float ran = Random.Range(0f, 1f);
		if (ran > 0.8)
		{
			Value = Random.Range(7, 10);
		}
		else
		{
			Value = Random.Range(1, 7);
		}
	}

	public void OnDragStart()
	{
		spriteRenderer.sortingOrder = 3;
		valueText.sortingOrder = 4;
	}

	public void OnDragEnd()
	{
		spriteRenderer.sortingOrder = 1;
		valueText.sortingOrder = 2;
	}

	/// <summary>
	/// Tint this block’s sprite to the given color.
	/// </summary>
	public void SetColor(Color col)
	{
		spriteRenderer.color = col;
	}

	public TextMeshPro ValueText
	{
		get { return valueText; }
		set { valueText = value; }
	}

	// public Color OriginalColor
	// {
		// get { return _originalColor; }
		// set { _originalColor = value; }
	// }

	/// <summary>Begin a looping pulse to show this block is in a potential clear.</summary>
	public void PlayPreview()
	{
		StopPreview();
		spriteRenderer.color = previewColor;
		//_previewRoutine = StartCoroutine(PreviewPulse());
	}

	/// <summary>Stop any preview animation on this block.</summary>
	public void StopPreview()
	{
		if (_previewRoutine != null)
			StopCoroutine(_previewRoutine);
		// restore to default
		transform.localScale = Vector3.one;
		// spriteRenderer.color = _originalColor;
	}

	private IEnumerator PreviewPulse()
	{
		var baseScale = Vector3.one;
		var maxScale = Vector3.one * 1.2f;
		while (true)
		{
			// scale up
			float t = 0f;
			while (t < 0.2f)
			{
				transform.localScale = Vector3.Lerp(baseScale, maxScale, t / 0.3f);
				t += Time.deltaTime;
				yield return null;
			}
			// scale down
			t = 0f;
			while (t < 0.2f)
			{
				transform.localScale = Vector3.Lerp(maxScale, baseScale, t / 0.3f);
				t += Time.deltaTime;
				yield return null;
			}
		}
	}
}