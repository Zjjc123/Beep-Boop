using UnityEngine;
using BeardedManStudios.Forge.Networking.Unity;

public class GameController : MonoBehaviour
{
    private void Start()
    {
        NetworkManager.Instance.InstantiatePlayer();
    }
}