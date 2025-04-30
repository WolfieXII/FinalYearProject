using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Runtime.CompilerServices;
using UnityEditor.Experimental.GraphView;



// REFERENCE: https://github.com/Unity-Technologies/ml-agents/blob/develop/docs/Learning-Environment-Create-New.md
// This agent was created using the above tutorial as reference, while used as a starting point various changes were made to reflect this projects experiments.
public class RollerAgent : Agent
{
    Rigidbody rBody;
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }


    public Transform Target;

    public GameObject RedKey;
    public GameObject BlueKey;
    public GameObject GreenKey;

    public GameObject RedDoor;
    public GameObject BlueDoor;
    public GameObject GreenDoor;

    public GameObject Trap1;
    public GameObject Trap2;

    //private bool hasKey = false;
    private KeyType? heldKey = null;

    private KeyType goalDoorType;

    private bool goalDoorOpened = false;

    private float previousDistanceToKey;
    private float previousDistanceToDoor;
    private bool initialized = false;

    private int stepCount = 0;
    public int maxStepCount = 5000;
    public override void OnEpisodeBegin()
    {

        initialized = false;
        heldKey = null;

        stepCount = 0;

        // Reset agent
        rBody.linearVelocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
        transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // Reset the Keys.
        BlueKey.SetActive(true);
        RedKey.SetActive(true);
        GreenKey.SetActive(true);

        // Reset the doors.
        RedDoor.SetActive(true);
        GreenDoor.SetActive(true);
        BlueDoor.SetActive(true);

        goalDoorOpened = false;

        // Randomize the keys postions.
        RedKey.transform.localPosition = new Vector3(Random.Range(-4f, -2f), 0.5f, -12f);
        BlueKey.transform.localPosition = new Vector3(Random.Range(-1f, 1f), 0.5f, 4f);
        GreenKey.transform.localPosition = new Vector3(Random.Range(-7f, -5f), 0.5f, -8f);

        //Logic for randomly selecting the door that will hold the goal behind it.
        goalDoorType = KeyType.Red;
        Debug.Log($"The goal is behind: {goalDoorType} door");

        // Just to make sure the traps spawn
        Trap1.SetActive(true);
        Trap2.SetActive(true);

        // Set up positions of the goal and traps
        Target.localPosition = new Vector3(0.023f, 0.5f, 12.94f);
        Target.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        Trap1.transform.localPosition = new Vector3(-13f, 0.05f, -0.59f);
        Trap1.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        Trap2.transform.localPosition = new Vector3(13.1f, 0.05f, -0.04f);
        Trap2.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);

        // Agent velocity
        sensor.AddObservation(rBody.linearVelocity.x);
        sensor.AddObservation(rBody.linearVelocity.z);

        // Door Positions
        sensor.AddObservation(RedDoor.transform.localPosition);
        sensor.AddObservation(GreenDoor.transform.localPosition);
        sensor.AddObservation(BlueDoor.transform.localPosition);

        // Key Positions
        sensor.AddObservation(RedKey.transform.localPosition);
        sensor.AddObservation(GreenKey.transform.localPosition);
        sensor.AddObservation(BlueKey.transform.localPosition);

        // Held Key
        sensor.AddObservation(heldKey.HasValue && heldKey == KeyType.Red ? 1f : 0f);
        sensor.AddObservation(heldKey.HasValue && heldKey == KeyType.Blue ? 1f : 0f);
        sensor.AddObservation(heldKey.HasValue && heldKey == KeyType.Green ? 1f : 0f);

    }

    public float forceMultiplier = 5f;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Allows the agent to move along the x & z axis in the enviroment.
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        rBody.AddForce(controlSignal * forceMultiplier);

        rBody.linearVelocity = Vector3.ClampMagnitude(rBody.linearVelocity, 3f);


        // Creating a timeout mechanic that once the agent reaches a certain amount of steps, end the episode and punish the agent.
        stepCount++;

        if(stepCount > maxStepCount)
        {
            AddReward(-5.0f);
            Debug.Log("Timeout. Ending episode.");
            EndEpisode();
        }

        // Rewards

        // Penalty to discourage idling.
        AddReward(-0.0005f);

        // When the episode starts get the positions of the correct key and door 
        if (!initialized)
        {
            previousDistanceToKey = Vector3.Distance(transform.localPosition, RedKey.transform.localPosition);
            previousDistanceToDoor = Vector3.Distance(transform.localPosition, RedDoor.transform.localPosition);
            initialized = true;
        }
        else
        {
            // Check to see if the agent has the correct key.
            if (!heldKey.HasValue)
            {
                // Get the current position of key.
                float currentDistanceToKey = Vector3.Distance(transform.localPosition, RedKey.transform.localPosition);

                // Get the distance to the correct key from the agent's position.
                float distanceChange = previousDistanceToKey - currentDistanceToKey;

                // Reward the agent if it is closer to the key, if not less rewards will be given.
                AddReward(distanceChange * 0.1f);

                // Update the position of the key to the current.
                previousDistanceToKey = currentDistanceToKey;
            }

            // Check if the agent has the red key.
            else if(heldKey.Value == KeyType.Red)
            {
                // Get the distance of the correct door.
                float currentDistanceToDoor = Vector3.Distance(transform.localPosition, RedDoor.transform.localPosition);

                // Get the distance to the correct door from the agent's position.
                float distanceChange = previousDistanceToDoor - currentDistanceToDoor;

                // Reward the agent if it gets closer to the door.
                AddReward(distanceChange * 0.2f);

                // Update the position.
                previousDistanceToDoor = currentDistanceToDoor;
            }
            else
            {
                // Penalize the agent the longer it takes to collect a key.
                AddReward(-0.02f);
            }
            
        }

        // Fell off platform
        if (this.transform.localPosition.y < 0)
        {
            // Punish the agent.
            AddReward(-5.0f);
            EndEpisode();
        }
    }
    // Allows the use of testing the scene using the arrow keys.
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    // Reference: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter.html
    public void OnTriggerEnter(Collider other)
    {
        // Check if the object that the agent collided with is a key.
        if (other.CompareTag("Key"))
        {
            // Check and get the type of key that has been picked up. (Red, Green, Blue)
            var newKey = other.GetComponent<KeyData>().keyType;
            //Debug.Log($"Picked up {heldKey}");

            // Save the currently held key into a variable.
            heldKey = newKey;

            // Check to see if the correct key had been picked up.
            if (heldKey == goalDoorType)
            {
                // Reward the agent.
                AddReward(5.0f);
                Debug.Log("Correct key has been picked up (+5)");
                // Deselect the other keys as the agent has learned to find the correct one.
                BlueKey.SetActive(false);
                GreenKey.SetActive(false);
            }
            else
            {
                // No change, dont want to avoid that area entirely.
                Debug.Log("Incorrect Key, no change.");
            }
            // Remove the key from the scene as it has been picked up.
            other.gameObject.SetActive(false);
        }

        if (other.CompareTag("Goal"))
        {
            Debug.Log("GOAL REACHED +10.0!");
            AddReward(10.0f);
            EndEpisode();
        }


        // Logic to end episode if the agent walks into the trap
        if (other.CompareTag("Trap"))
        {
            Debug.Log("Trap tocuhed. Ending the episode");
            AddReward(-5.0f);
            EndEpisode();
        }
    }

    // Reference: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnCollisionEnter.html
    public void OnCollisionEnter(Collision collision)
    {
        // Check to see if the agent collided with a door.
        if (collision.gameObject.CompareTag("Door"))
        {
            // Get the type of door the agent encountered (Red, Green, Blue)
            DoorData door = collision.gameObject.GetComponent<DoorData>();

            // Log what door is being touched.
            Debug.Log($"Touching {door}");

            // Check if the agent doesn't have a key.
            if (!heldKey.HasValue)
            {
                // Just to check if the collision was registered.
                Debug.Log("Touched door without a key.");

            }
            // If it has a key.
            else
            {
                // If a key colour matches with the door colour.
                if (heldKey.Value == door.doorType)
                {
                    // "Open the door".
                    collision.gameObject.SetActive(false);

                    // If the door that has been opened is the door holding the goal.
                    if (door.doorType == goalDoorType)
                    {
                        // Reward the agent for finding out how to open the correct door.
                        Debug.Log("UNLOCKED GOAL DOOR (+15)");
                        goalDoorOpened = true;
                        AddReward(15.0f);
                    }
                    else
                    {
                        // Punish the agent for unlocking the wrong door.
                        AddReward(-3.0f);
                        Debug.Log("Unlocked the wrong door!!");
                    }
                }
                // If the colour of the key does not match with the door.
                else
                {
                    // Punish the agent for wrong colour combination.
                    Debug.Log("Touched wrong door with the wrong key (-3)");
                    AddReward(-3.0f);
                }
            }
        }
    }
}
