using UnityEngine;

// Logic for setting the type of key for the key objects the agent will be collecting (Red, Green or Blue).
public enum KeyType { Red, Green, Blue }
public class KeyData : MonoBehaviour
{
    public KeyType keyType;
}
