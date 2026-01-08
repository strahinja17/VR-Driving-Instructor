#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine;

public class MicPermission : MonoBehaviour
{
    void Awake()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
    }
}
