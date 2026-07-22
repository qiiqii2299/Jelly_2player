using UnityEngine;

public class SpiderManController : PlayerBase
{
    [Header("Cấu hình Bắn Tơ Spider-Man")]
    public Transform firePoint;
    public GameObject webEffectPrefab;
    public GameObject webDartPrefab;

    [Header("Vật lý tơ nhện")]
    public float maxWebDistance = 15f;
    public LayerMask webLayer;

    private DistanceJoint2D distanceJoint;
    private LineRenderer lineRenderer;
    private GameObject activeWebDart;

    protected override void Start()
    {
        base.Start();
        distanceJoint = GetComponent<DistanceJoint2D>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    protected override void HandleSkillInput()
    {
        // 1. BẤM CHUỘT PHẢI: Buông tơ hoàn toàn, trả lại trạng thái di chuyển/rơi bình thường
        if (Input.GetMouseButtonDown(1))
        {
            StopWebShoot();
            return;
        }

        // 2. BẤM CHUỘT TRÁI: 
        // - Bấm lần 1: Bắn tơ găm vào vị trí trỏ chuột và giữ lại cố định.
        // - Bấm lần 2 (đang bám mà bấm chỗ khác): Ngắt tơ cũ, lập tức chuyển sang điểm mới.
        if (Input.GetMouseButtonDown(0))
        {
            ShootWebToCursor();
        }

        // Nếu đang bám tơ, khóa vận tốc tuyệt đối để nhân vật đứng yên trên không/tường
        if (isGrappling)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ShootWebToCursor()
    {
        // Lấy tọa độ chuột chính xác trong thế giới game
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Lấy vị trí tâm nhân vật (nhích lên ngực một chút) làm gốc xuất phát để không bị lệch hướng khi lật mặt
        Vector2 originPos = (Vector2)transform.position + new Vector2(0f, 0.5f);

        // Tính vector hướng chuẩn xác từ nhân vật tới vị trí chuột
        Vector2 fireDirection = (mousePos - originPos).normalized;

        // Bắn Raycast theo đúng hướng chuột với tầm với maxWebDistance
        RaycastHit2D hit = Physics2D.Raycast(originPos, fireDirection, maxWebDistance, webLayer);

        if (hit.collider != null)
        {
            // Nếu đang bám tơ ở chỗ cũ mà bấm sang chỗ mới, ngắt tơ cũ ngầm để đổi điểm mượt mà
            if (isGrappling)
            {
                StopWebShootQuietly();
            }

            StartWebShoot(hit.point);
        }
    }

    private void StartWebShoot(Vector2 hitPoint)
    {
        isGrappling = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // Khóa vật lý động để giữ nhân vật đứng yên bất động

        // Tạo hiệu ứng chớp sáng khi tơ dính vào tường (tự hủy sau 0.5s tránh tràn bộ nhớ)
        if (webEffectPrefab != null)
        {
            GameObject effect = Instantiate(webEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(effect, 0.5f);
        }

        // Cấu hình khoảng cách dây
        distanceJoint.enabled = true;
        distanceJoint.connectedAnchor = hitPoint;
        distanceJoint.distance = Vector2.Distance((Vector2)transform.position + new Vector2(0f, 0.5f), hitPoint);

        // Bật hiển thị sợi tơ
        lineRenderer.enabled = true;

        // Quản lý đối tượng ghim tơ tại bề mặt trúng
        if (activeWebDart == null)
        {
            activeWebDart = Instantiate(webDartPrefab, hitPoint, Quaternion.identity);
        }
        else
        {
            activeWebDart.transform.position = hitPoint;
            activeWebDart.SetActive(true);
        }
    }

    // Ngắt tơ và trả lại trạng thái rơi/chạy tự động (Dùng khi bấm chuột phải)
    private void StopWebShoot()
    {
        isGrappling = false;
        rb.bodyType = RigidbodyType2D.Dynamic; // Trả lại vật lý thông thường cho nhân vật

        if (distanceJoint != null && distanceJoint.enabled)
        {
            distanceJoint.enabled = false;
            lineRenderer.enabled = false;

            if (activeWebDart != null)
            {
                activeWebDart.SetActive(false);
            }
        }
    }

    // Ngắt tơ ngầm không đổi BodyType (Dùng khi chuyển đổi điểm bám liên tục)
    private void StopWebShootQuietly()
    {
        if (distanceJoint != null && distanceJoint.enabled)
        {
            distanceJoint.enabled = false;
            lineRenderer.enabled = false;

            if (activeWebDart != null)
            {
                activeWebDart.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        // Cập nhật co giãn vị trí 2 đầu sợi tơ liên tục theo chuyển động của nhân vật
        if (lineRenderer.enabled)
        {
            Vector2 startPos = (Vector2)transform.position + new Vector2(0f, 0.5f);
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, distanceJoint.connectedAnchor);
        }
    }
}