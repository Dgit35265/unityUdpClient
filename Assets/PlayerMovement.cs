using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private void Update()
    {     
        if (Input.GetKey("w"))
        {
            //transform.position += transform.localToWorldMatrix()
            transform.Translate(new Vector3(0, 0, 3 * Time.deltaTime));
        }
        if (Input.GetKey("s"))
        {
            transform.Translate(new Vector3(0, 0, -3 * Time.deltaTime));
        }
        if (Input.GetKey("a"))
        {
            transform.Translate(new Vector3(-3 * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey("d"))
        {
            transform.Translate(new Vector3(3 * Time.deltaTime, 0, 0));
        }
    }
}
