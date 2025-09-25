using UnityEngine;

public class NoiseImpulse : MonoBehaviour
{
    public void Fire(float add01)
    {
        if (NoiseSystem.Instance) NoiseSystem.Instance.FireImpulse(add01);
    }
}
