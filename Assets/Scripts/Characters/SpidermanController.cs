using UnityEngine;

public class SpiderManController : PlayerBase
{
    [Header("Cấu hình Bắn Tơ Spider-Man")]
    public Transform firePoint;
    public GameObject webEffectPrefab;
    public GameObject webDartPrefab;

    [Header("Vật lý tơ nhện & Đu đưa")]
    public float maxWebDistance = 15f;
    public LayerMask webLayer;
    public float swingForce = 20f; // Lực đẩy qua lại khi bấm phím A/D

    [Header("Cấu hình Thu ngắn dây (Giống Batman)")]
    public float climbSpeed = 4f;      // Tốc độ tự động thu ngắn dây
    public float minRopeLength = 1.5f; // Chiều dài dây ngắn nhất có thể rút

    private DistanceJoint2D distanceJoint;
    private LineRenderer lineRenderer;
    private GameObject activeWebDart;
    private Animator animator; // Biến quản lý Animator để gọi hiệu ứng lật mặt

    protected override void Start()
    {
        base.Start();

        // Tự tạo DistanceJoint2D nếu chưa có trên object
        distanceJoint = GetComponent<DistanceJoint2D>();
        if (distanceJoint == null)
            distanceJoint = gameObject.AddComponent<DistanceJoint2D>();
        distanceJoint.enabled = false;
        distanceJoint.enableCollision = true;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.enabled = false;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;

        animator = GetComponent<Animator>();
    }

    protected override void HandleSkillInput()
    {
        // 1. BẤM SPACE: Nếu đang đu → thả tơ. Nếu không → bắn tơ vào vị trí chuột
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrappling)
                StopWebShoot();
            else
                ShootWebToCursor();
            return;
        }

        // 2. XỬ LÝ KHI ĐANG BÁM TƠ: Tự động thu ngắn dây và cho phép đung đưa
        if (isGrappling)
        {
            // Tự động thu ngắn dây kéo nhân vật lại gần điểm bám (giống Batman)
            if (distanceJoint.distance > minRopeLength)
            {
                distanceJoint.distance -= climbSpeed * Time.deltaTime;
            }

            // Đánh đu qua lại bằng phím mũi tên trái/phải
            float swingInput = Input.GetAxisRaw("Horizontal");
            if (swingInput != 0)
            {
                rb.AddForce(new Vector2(swingInput * swingForce, 0f));
                UpdateFacingDirection(swingInput);
            }
        }
        else
        {
            // 3. KHI DI CHUYỂN BÌNH THƯỜNG: lật mặt theo phím
            float moveInput = Input.GetAxisRaw("Horizontal");
            if (moveInput != 0)
                UpdateFacingDirection(moveInput);
        }
    }

    // Hàm chung xử lý hướng mặt và gọi Animation Turn cho cả lúc bám tơ lẫn lúc đi bình thường
    private void UpdateFacingDirection(float inputDirection)
    {
        if (inputDirection > 0)
        {
            if (transform.rotation.eulerAngles.y != 0) // Chỉ lật khi đổi hướng thực sự
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
                TriggerFlipAnimation();
            }
        }
        else if (inputDirection < 0)
        {
            if (transform.rotation.eulerAngles.y != 180) // Chỉ lật khi đổi hướng thực sự
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
                TriggerFlipAnimation();
            }
        }
    }

    // Hàm kích hoạt animation lật mặt an toàn
    private void TriggerFlipAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Turn"); // Kích hoạt Trigger "Turn" trong Animator Controller
        }
    }

    private void ShootWebToCursor()
    {
        // Lấy tọa độ chuột chính xác trong thế giới game
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Lấy gốc xuất phát từ vị trí thực tế của firePoint (ở tay nhân vật)
        Vector2 originPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        // Tính vector hướng chuẩn xác từ tay nhân vật tới vị trí chuột
        Vector2 fireDirection = (mousePos - originPos).normalized;

        // Bắn Raycast — nếu webLayer chưa set thì bắn vào tất cả
        RaycastHit2D hit = webLayer != 0
            ? Physics2D.Raycast(originPos, fireDirection, maxWebDistance, webLayer)
            : Physics2D.Raycast(originPos, fireDirection, maxWebDistance);

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

        // GIỮ VẬT LÝ DYNAMIC: Để nhân vật có trọng lực và văng được, không dùng Kinematic nữa
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Tạo hiệu ứng chớp sáng khi tơ dính vào tường (tự hủy sau 0.5s)
        if (webEffectPrefab != null)
        {
            GameObject effect = Instantiate(webEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(effect, 0.5f);
        }

        // Cấu hình khoảng cách dây xuất phát từ firePoint
        Vector2 originPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        distanceJoint.enabled = true;
        distanceJoint.connectedAnchor = hitPoint;
        distanceJoint.distance = Vector2.Distance(originPos, hitPoint) * 0.9f; // Thu ngắn một chút để tạo độ căng cho dây

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
        rb.bodyType = RigidbodyType2D.Dynamic;

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

    // Ngắt tơ ngầm không ngắt trạng thái đu (Dùng khi chuyển đổi điểm bám liên tục)
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
        // Cập nhật co giãn vị trí 2 đầu sợi tơ bám chính xác từ firePoint tới điểm neo
        if (lineRenderer.enabled)
        {
            Vector2 startPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, distanceJoint.connectedAnchor);
        }
    }
}