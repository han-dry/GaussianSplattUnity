using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float fastMoveMultiplier = 4f;
    public float slowMoveMultiplier = 0.25f;
    public float climbSpeed = 5f;

    [Header("Look")]
    public float lookSensitivity = 2f;
    public bool lockCursorOnPlay = true;

    float rotationX;
    float rotationY;

    void Start()
    {
        Vector3 rot = transform.eulerAngles;
        rotationY = rot.x;
        rotationX = rot.y;

        if (lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {

        rotationX += Input.GetAxis("Mouse X") * lookSensitivity;
        rotationY -= Input.GetAxis("Mouse Y") * lookSensitivity;
        rotationY = Mathf.Clamp(rotationY, -89f, 89f);

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);

        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= fastMoveMultiplier;
        if (Input.GetKey(KeyCode.LeftAlt)) speed *= slowMoveMultiplier;

        Vector3 move = Vector3.zero;
        move += transform.forward * Input.GetAxisRaw("Vertical");
        move += transform.right * Input.GetAxisRaw("Horizontal");

        if (Input.GetKey(KeyCode.Space)) move += Vector3.up * climbSpeed;
        if (Input.GetKey(KeyCode.LeftControl)) move += Vector3.down * climbSpeed;

        transform.position += move.normalized * speed * Time.deltaTime;
    }
}