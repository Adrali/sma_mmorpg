using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    //Privates
    private float xInput = 0; //Q et D
    private float zInput = 0; //Z et S
    private Vector3 mouvement = Vector3.zero;
    private float speed = 10; //Vitesse en l'air
    private Rigidbody cameraRigidbody;

    private float xMouse = 0; //Souris "horizontale"
    private float yMouse = 0; //Souris "verticale"
    private float rotX = 0; //Angle actuel
    private float rotY = 0;
    private float mouseSensitivity = 1000; //Vitesse de la souris

    private void Awake()
    {
        cameraRigidbody = gameObject.GetComponent<Rigidbody>(); //On recupere le rigidbody
        Cursor.lockState = CursorLockMode.Locked; //On bloque le curseur dans la fenetre de jeu
    }

    void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        zInput = Input.GetAxisRaw("Vertical");

        xMouse = Input.GetAxisRaw("Mouse X");
        yMouse = Input.GetAxisRaw("Mouse Y");
    }

    private void FixedUpdate()
    {
        mouvement = (transform.right * xInput + transform.forward * zInput);
        mouvement.y = 0;
        cameraRigidbody.MovePosition(cameraRigidbody.position + mouvement.normalized * speed * Time.fixedDeltaTime);

        rotX -= yMouse * (mouseSensitivity / 2f) * Time.fixedDeltaTime;
        rotX = Mathf.Clamp(rotX, -80f, 80f);
        rotY += xMouse * mouseSensitivity * Time.fixedDeltaTime;
        transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
    }
}
