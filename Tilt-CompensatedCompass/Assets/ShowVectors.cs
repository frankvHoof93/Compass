using UnityEngine;

public class ShowVectors : MonoBehaviour {
    public int size;
    public bool ShowUp = true;
    public bool ShowDown = true;
    public bool ShowLeft = true;
    public bool ShowRight = true;
    public bool ShowForward = true;
    public bool ShowBack = true;
    public Color ColorUp;
    public Color ColorDown;
    public Color ColorLeft;
    public Color ColorRight;
    public Color ColorForward;
    public Color ColorBack;
    

    void OnDrawGizmos () {
        
        if (ShowRight)
        {
            ColorRight.a = 255;
            Gizmos.color = ColorRight;          
            Gizmos.DrawLine(transform.position, transform.position + transform.right * size);
        }
        if (ShowLeft)
        {
            ColorLeft.a = 255;
            Gizmos.color = ColorLeft;
            Gizmos.DrawLine(transform.position, transform.position - transform.right * size);
        }
        if (ShowForward)
        {
            ColorForward.a = 255;
            Gizmos.color = ColorForward; 
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * size);
        }
        if (ShowBack)
        {
            ColorBack.a = 255;
            Gizmos.color = ColorBack;
            Gizmos.DrawLine(transform.position, transform.position - transform.forward * size);
        }
        if (ShowUp)
        {
            ColorUp.a = 255;
            Gizmos.color = ColorUp;
            Gizmos.DrawLine(transform.position, transform.position + transform.up * size);
        }
        if (ShowDown)
        {
            ColorDown.a = 255;
            Gizmos.color = ColorDown;
            Gizmos.DrawLine(transform.position, transform.position - transform.up * size);
        }
	}
}
