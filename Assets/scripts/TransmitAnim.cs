using Managers;
using UnityEngine;

public class TransmitAnim : MonoBehaviour
{
    public Vector3 target;
    public float lifetime = 1.0f;
    public float distance = 1.0f;

    private float age = 0f;

    void Start()
    {
        lifetime = NetworkStateManager.Instance.tickRate;
    }
    void Update()
    {
        age += Time.deltaTime;
        if(age>= lifetime)
        {
            Destroy(gameObject);
        }
        //do some animation
        float delta = Time.deltaTime / lifetime;
        
        if(target == null) return;
        transform.LookAt(target);
        transform.position = Vector3.MoveTowards(transform.position, target, delta * distance);
    }
    public void SetDestination(Vector3 _target)
    {
        target = _target;
        distance = Vector3.Distance(transform.position, target);
    }
}