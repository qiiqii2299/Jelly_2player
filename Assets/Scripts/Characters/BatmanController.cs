using UnityEngine;

public class BatmanController : PlayerBase
{
    [Header("Cấu hình Đu dây")]
    public Transform firePoint;
    public GameObject sideEffectPrefab;
    public GameObject dartPrefab;

    [Header("Vật lý dây móc")]
    public float maxGrappleDistance = 15f;
    public LayerMask grappleLayer;

    [Header("Điều khiển khi đu dây")]
    public float climbSpeed = 4f;      // Tốc độ tự động thu ngắn dây
    public float swingForce = 20f;     // Lực đẩy qua lại khi bấm phím
    public float minRopeLength = 1.5f; // Chiều dài dây ngắn nhất

    private DistanceJoint2D distanceJoint;
    private LineRenderer lineRenderer;
    private GameObject activeDart;

    // Cờ kiểm soát chống lỗi kẹt dây khi nhả chuột quá nhanh
    private bool isAttemptingGrapple = false;

    protected override void Start()
    {
        base.Start();
        distanceJoint = GetComponent<DistanceJoint2D>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    protected override void HandleSkillInput()
    {
        // 1. LUÔN LẮNG NGHE SỰ KIỆN BUÔNG TAY: Dù đang đu hay không, thả chuột là ngắt dây ngay lập tức
        if (Input.GetMouseButtonUp(0))
        {
            isAttemptingGrapple = false;
            StopGrapple();
            return;
        }

        // 2. NẾU ĐANG ĐU DÂY: Xử lý thu dây, đánh đu và nhảy thoát bằng Space
        if (isGrappling)
        {
            // Tự động thu ngắn dây kéo nhân vật lên
            if (distanceJoint.distance > minRopeLength)
            {
                distanceJoint.distance -= climbSpeed * Time.deltaTime;
            }

            // Đánh đu qua lại bằng phím A/D hoặc mũi tên trái/phải
            float swingInput = Input.GetAxisRaw("Horizontal");
            if (swingInput != 0)
            {
                rb.AddForce(new Vector2(swingInput * swingForce, 0f));

                // Xoay mặt nhân vật theo hướng văng
                if (swingInput > 0)
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                else if (swingInput < 0)
                    transform.rotation = Quaternion.Euler(0, 180, 0);
            }

            // Bấm Space để cắt dây và tung người nhảy vọt lên cao bám vào bờ gạch
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isAttemptingGrapple = false;
                StopGrapple();
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }

            return;
        }

        // 3. NẾU CHƯA ĐU DÂY: Xử lý bấm giữ chuột để ném phi tiêu
        if (Input.GetMouseButtonDown(0))
        {
            isAttemptingGrapple = true;
            anim.SetTrigger("Throw");
        }
    }

    // Gọi qua Animation Event tại frame vung tay xa nhất
    public void ExecuteThrow()
    {
        // Hủy bắn dây nếu người chơi đã buông chuột trước khi hoạt ảnh ném kết thúc
        if (!isAttemptingGrapple) return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 fireDirection = (mousePos - (Vector2)firePoint.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, fireDirection, maxGrappleDistance, grappleLayer);

        if (hit.collider != null)
        {
            StartGrapple(hit.point);
        }
    }

    private void StartGrapple(Vector2 hitPoint)
    {
        // Bật cờ trạng thái để báo cho PlayerBase ngừng chạy tự động
        isGrappling = true;

        // Sinh effect tại điểm trúng (phi tiêu)
        if (sideEffectPrefab != null)
        {
            Instantiate(sideEffectPrefab, hitPoint, Quaternion.identity);
        }

        // Bật vật lý Distance Joint
        distanceJoint.enabled = true;
        distanceJoint.connectedAnchor = hitPoint;
        distanceJoint.distance = Vector2.Distance(firePoint.position, hitPoint) * 0.8f;

        // Bật hình ảnh dây
        lineRenderer.enabled = true;

        // Sinh phi tiêu
        if (activeDart == null)
        {
            activeDart = Instantiate(dartPrefab, hitPoint, Quaternion.identity);
        }
        else
        {
            activeDart.transform.position = hitPoint;
            activeDart.SetActive(true);
        }
    }

    private void StopGrapple()
    {
        // Tắt cờ trạng thái để PlayerBase tiếp tục chạy tự động
        isGrappling = false;

        if (distanceJoint != null && distanceJoint.enabled)
        {
            distanceJoint.enabled = false;
            lineRenderer.enabled = false;

            if (activeDart != null)
            {
                activeDart.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        // Cập nhật vị trí 2 đầu dây liên tục khi đang đu
        if (lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, firePoint.position);
            lineRenderer.SetPosition(1, distanceJoint.connectedAnchor);
        }
    }
}