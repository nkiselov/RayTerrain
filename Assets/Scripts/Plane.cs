using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plane : MonoBehaviour
{
    public float maxSpeed;
	public float acceleration;
	public float rotationSpeed;
    private float speed;

    public void Update()
	{
		if (Input.GetKey(KeyCode.X))
		{
			speed = 0;
		}
        if (Input.GetKey(KeyCode.Z))
		{
			speed = maxSpeed;
		}
		if (Input.GetKey(KeyCode.LeftShift))
		{
			speed = Mathf.Min(speed + acceleration, maxSpeed);
		}
		if (Input.GetKey(KeyCode.LeftControl))
		{
			speed = Mathf.Max(speed - acceleration, 0);
		}
		float dy = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
		float dx = -Input.GetAxis("Vertical") * rotationSpeed * Time.deltaTime;
		float dz = 0;
		if (Input.GetKey(KeyCode.Q))
		{
			dz = rotationSpeed * Time.deltaTime;
		}
		if (Input.GetKey(KeyCode.E))
		{
			dz = -rotationSpeed * Time.deltaTime;
		}
		transform.Rotate(dx, dy, dz, Space.Self);
		transform.position += transform.forward * speed * Time.deltaTime;
	}
}
