using UnityEngine;

namespace Nakatani
{
    public class TankAutoSetup : MonoBehaviour
    {
        void Awake()
        {
            var initializer = GetComponent<TankInitializer>();
            if (initializer != null)
            {
                initializer.Setup(true);
            }
        }
    }
}