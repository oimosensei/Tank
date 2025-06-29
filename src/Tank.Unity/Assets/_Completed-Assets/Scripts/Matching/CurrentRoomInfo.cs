using UnityEngine;

namespace Nakatani.Matching
{
    public class CurrentRoomInfo : MonoBehaviour
    {
        private static CurrentRoomInfo _instance;
        public static CurrentRoomInfo Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    var go = new GameObject("CurrentRoomInfo");
                    _instance = go.AddComponent<CurrentRoomInfo>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public RoomInfo RoomInfo { get; set; }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}