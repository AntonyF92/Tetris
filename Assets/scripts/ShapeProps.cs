using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class ShapeProps : MonoBehaviour
{
    [SerializeField]
    private bool limitTurns = false;
    [SerializeField]
    private bool canTurn = true;

    [SerializeField] private int rotationAngle = 90;

    void RotateImpl(int degree)
    {
        if (limitTurns)
        {
            if ((int) transform.rotation.eulerAngles.z == 0)
            {
                transform.eulerAngles = new Vector3(0, 0, degree);
            }
            else
            {
                transform.eulerAngles = new Vector3(0, 0, 0);                
            }
        }
        else
        {
            transform.Rotate(new Vector3(0, 0, degree));
        }
    }
    
    public void RotateRight()
    {
        if (!canTurn)
            return;
        RotateImpl(-rotationAngle);
    }

    public void RotateLeft()
    {
        RotateImpl(rotationAngle);
    }

    public Vector2 LowestPoint()
    {
        Vector2 result = new Vector2(transform.position.x, transform.position.y);
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.position.y < result.y)
            {
                result = child.position;
            }
        }

        return result;
    }
}
