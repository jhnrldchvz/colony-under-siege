using UnityEngine;
using TMPro;

public class ObjectivePickup : MonoBehaviour
{
    [Header("Identity")]
    public string itemId      = "data_core_01";
    public string displayName = "Data Core";

    [Header("Rotation")]
    public float rotateSpeed    = 60f;

    [Header("Float")]
    public float floatAmplitude = 0.2f;
    public float floatSpeed     = 1.5f;

    [Header("Proximity Label")]
    public Canvas          labelCanvas;
    public TextMeshProUGUI labelText;
    public float           labelRange = 5f;
    public float           fadeSpeed  = 8f;

    [Header("Quick Outline")]
    public Outline outline;

    private Vector3     _startPos;
    private float       _phase;
    private Transform   _player;
    private Camera      _cam;
    private CanvasGroup _cg;
    private bool        _collected = false;

    private void Start()
    {
        _startPos = transform.position;
        _phase    = Random.Range(0f, Mathf.PI * 2f);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        _cam = Camera.main;

        if (labelCanvas != null)
        {
            _cg       = labelCanvas.gameObject.GetComponent<CanvasGroup>();
            if (_cg == null) _cg = labelCanvas.gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            labelCanvas.gameObject.SetActive(false);
        }

        if (labelText != null)
            labelText.text = $"{displayName}\n<size=70%>Walk to collect</size>";

        if (outline != null) outline.enabled = false;
    }

    private void Update()
    {
        if (_collected) return;

        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);

        float newY = _startPos.y +
                     Mathf.Sin((Time.time + _phase) * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);

        if (_player == null) return;
        float dist = Vector3.Distance(transform.position, _player.position);
        bool  near = dist <= labelRange;

        if (outline != null) outline.enabled = near;

        if (_cg != null)
        {
            _cg.alpha = Mathf.Lerp(_cg.alpha, near ? 1f : 0f,
                                   Time.deltaTime * fadeSpeed);
            bool active = _cg.alpha > 0.01f;
            if (labelCanvas.gameObject.activeSelf != active)
                labelCanvas.gameObject.SetActive(active);
            if (_cam != null && active)
                labelCanvas.transform.LookAt(
                    labelCanvas.transform.position + _cam.transform.forward);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;
        if (InventoryManager.Instance == null) return;

        _collected = true;
        InventoryManager.Instance.AddKeyItem(itemId);

        // UIManager.OnKeyItemCollected routes by itemId automatically  
        // For generic key items (not power cells / deactivation tool),
        // also pass the display name so HUD shows correct label
        UIManager.Instance?.ShowKeyItemByIdAndName(itemId, displayName);

        Debug.Log($"[ObjectivePickup] Collected '{displayName}' (id: {itemId})");
        Destroy(gameObject);
    }
}