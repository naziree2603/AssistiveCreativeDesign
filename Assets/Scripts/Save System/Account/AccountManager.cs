    using UnityEngine;

    public class AccountManager : MonoBehaviour
    {
        public static AccountManager Instance;

        public AccountData CurrentAccount;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }