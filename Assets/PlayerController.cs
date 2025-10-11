using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
 
//This is made by Bobsi Unity - Youtube
public class PlayerController : NetworkBehaviour
{
    [Header("Base setup")]
    public float walkingSpeed = 0f;
    public float runningSpeed = 0f;
    public float jumpSpeed = 0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;
 
    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;
 
    [HideInInspector]
    public bool canMove = true;
 
    [SerializeField]
    private float cameraYOffset = 2f;
    private Camera playerCamera;
 
 
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (base.IsOwner)
        {
            CreatePlayerCamera();
        }
        else
        {
            gameObject.GetComponent<PlayerController>().enabled = false;
        }
    }

    private void CreatePlayerCamera()
    {
        // Certifica-se de que a câmera ainda não existe
        if (playerCamera == null)
        {
            // Cria uma nova câmera
            playerCamera = new GameObject("PlayerCamera").AddComponent<Camera>();
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y + cameraYOffset, transform.position.z);
            playerCamera.transform.SetParent(transform);
        }
    }
 
    void Start()
    {
        characterController = GetComponent<CharacterController>();
 
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Obtendo a posição atual do objeto
        Vector3 currentPosition = transform.position;

        // Movendo o objeto para cima ao longo do eixo Y
        currentPosition.y += 1.0f;

        // Aplicando a nova posição ao objeto
        transform.position = currentPosition;
    }
 
    void Update()
    {
        bool isRunning = false;
 
        // Press Left Shift to run
        isRunning = Input.GetKey(KeyCode.LeftShift);
 
        // We are grounded, so recalculate move direction based on axis
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
 
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);
 
        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }
 
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
 
        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);
 
        // Player and Camera rotation
        if (canMove && playerCamera != null && !Cursor.visible)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorVisibility();
        }
    }

    // Function to toggle cursor visibility
    void ToggleCursorVisibility()
    {
        // If the cursor is currently locked, unlock it and make it visible
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        // If the cursor is currently unlocked, lock it and make it invisible
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        // Este método é chamado quando o objeto é desativado, incluindo desconexões
        if (base.IsClient && IsLocalPlayer())
        {
            // Certifica-se de liberar recursos quando o jogador é desconectado
            CleanUpOnDisconnect();
        }
    }

    private void CleanUpOnDisconnect()
    {
        // Put your code here to clean up player-specific resources when they are disconnected
        // For example, you can destroy the camera here if it still exists
        if (playerCamera != null)
        {
            Destroy(playerCamera.gameObject);
            playerCamera = null; // Certifica-se de que a referência seja nula após a destruição
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (base.IsClient && IsLocalPlayer())
        {
            // O jogador está prestes a ser desconectado, chame a função de limpeza
            CleanUpOnDisconnect();
        }
    }

    // Check if the player has authority
    private bool IsLocalPlayer()
    {
        // Replace this line with the appropriate method used by FishNet to check for authority
        // For example, if FishNet uses something like 'IsLocalPlayer', replace the condition accordingly
        return base.IsOwner;
    }
}