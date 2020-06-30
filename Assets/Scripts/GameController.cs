using UnityEngine;
using BeardedManStudios.Forge.Networking.Unity;
using BeardedManStudios.Forge.Networking.Generated;
using System.Collections.Generic;

public class GameController : GameControllerBehavior
{
    /*
     
    networkObject.MyPlayerId --> My networking player ID
    player.NetworkID --> My networking player ID

    networkObject.Owner.NetworkID --> Owner's networking player ID

    networkObject.NetworkID --> Object Network ID


    */

    // Singleton instance
    public static GameController Instance;

    // List of players
    // List of players
    private readonly Dictionary<uint, PlayerBehavior> _playerObjects = new Dictionary<uint, PlayerBehavior>();

    private bool _networkReady;

    private bool isServerPlayer = true;

    // Keeping 1 instance
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
        DontDestroyOnLoad(Instance);
    }

    protected override void NetworkStart()
    {
        base.NetworkStart();

        if (NetworkManager.Instance.IsServer)
        {
            if (isServerPlayer)
            {
                PlayerBehavior p = NetworkManager.Instance.InstantiatePlayer();
                _playerObjects.Add(p.networkObject.MyPlayerId, p);
                Debug.Log("Server Player " + p.networkObject.MyPlayerId + " Joined");
            }

            NetworkManager.Instance.Networker.playerAccepted += (player, sender) =>
            {
                // Instantiate the player on the main Unity thread, get the Id of its owner and add it to a list of players
                MainThreadManager.Run(() =>
                {
                    PlayerBehavior p = NetworkManager.Instance.InstantiatePlayer();
                    p.networkObject.AssignOwnership(player);
                    _playerObjects.Add(player.NetworkId, p);
                    Debug.Log("Player " + player.NetworkId + " Joined");
                });

                /* For limiting & waiting on players
                if (playerCount == 2)
                {
                    // Not sure if this actally works. It should switch off the server accepting new players
                    ((IServer)NetworkManager.Instance.Networker).StopAcceptingConnections();

                    // Now start the game
                    GameStart();
                }
                */

            };

            NetworkManager.Instance.Networker.playerDisconnected += (player, sender) =>
            {
                // Remove the player from the list of players and destroy it
                PlayerBehavior p = _playerObjects[player.NetworkId];
                _playerObjects.Remove(player.NetworkId);
                p.networkObject.Destroy();
                Debug.Log("Player " + player.NetworkId + " Left");
            };
        }

        _networkReady = true;
    }

    // Force NetworkStart to happen - a work around for NetworkStart not happening
    // for objects instantiated in scene in the latest version of Forge
    private void FixedUpdate()
    {
        if (!_networkReady && networkObject != null)
        {
            NetworkStart();
        }
    }
}
 