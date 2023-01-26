using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cameraTransform;

    public float minMovementSpeed;
    public float maxMovementSpeed;
    public float rotationAmount;
    public float zoomAmount;
    public float maxZoom;
    public float minZoom;
    public float movementTime;

    private Vector3 newPosition;
    private Quaternion newRotation;
    private Vector3 newZoom;

    public float movementSpeed;

    // Start is called before the first frame update
    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovementInput();
    }

    void HandleMovementInput()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            newPosition += (transform.forward * movementSpeed);
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            newPosition += (transform.forward * -movementSpeed);
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            newPosition += (transform.right * movementSpeed);
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            newPosition += (transform.right * -movementSpeed);

        if (Input.GetKey(KeyCode.Q))
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        if (Input.GetKey(KeyCode.E))
            newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);

        if (Input.GetKey(KeyCode.R))
        {
            newZoom += new Vector3(0, -zoomAmount, zoomAmount);
            
            if (newZoom.y < minZoom)
            {
                newZoom.y = minZoom;
                newZoom.z = -minZoom;
            }
        }

        if (Input.GetKey(KeyCode.F))
        {
            newZoom -= new Vector3(0, -zoomAmount, zoomAmount);

            if (newZoom.y > maxZoom)
            {
                newZoom.y = maxZoom;
                newZoom.z = -maxZoom;
            }
        }

        movementSpeed = minMovementSpeed + (maxMovementSpeed - minMovementSpeed) * (newZoom.y - minZoom) / maxZoom;

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }
}
