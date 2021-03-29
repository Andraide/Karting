using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public Circuit circuit;
    public float brakingSensitivity = 3f;
    Drive ds;
    public float steeringSensitivity = 0.01f;
    public float accelSensitivity = 0.3f;
    Vector3 target;
    Vector3 nextTarget;
    int currentWP = 0;
    float totalDistanceToTarget;
    
    GameObject tracker;
    int currentTrackerWp = 0;
    float lookAhead = 10;

    // Start is called before the first frame update
    void Start()
    {
        ds = this.GetComponent<Drive>();
        target = circuit.waypoints[currentWP].transform.position;
        nextTarget = circuit.waypoints[currentWP + 1].transform.position;
        totalDistanceToTarget = Vector3.Distance(target, ds.rb.gameObject.transform.position);

        tracker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        DestroyImmediate(tracker.GetComponent<Collider>());
        tracker.transform.position = ds.rb.gameObject.transform.position;
        tracker.transform.rotation = ds.rb.gameObject.transform.rotation;
    }

    void ProgressTracker()
    {
        Debug.DrawLine(ds.rb.gameObject.transform.position, tracker.transform.position);
        if(Vector3.Distance(ds.rb.gameObject.transform.position, tracker.transform.position) > lookAhead) return;
        
        tracker.transform.LookAt(circuit.waypoints[currentTrackerWp].transform.position);
        tracker.transform.Translate(0, 0, 1.0f); //Speed of tracker

        if(Vector3.Distance(tracker.transform.position, circuit.waypoints[currentTrackerWp].transform.position) < 1)
        { 
            currentTrackerWp++;
            if(currentTrackerWp >= circuit.waypoints.Length)
                currentTrackerWp = 0;
        }
    }

    // Update is called once per frame
    bool isJump = false;
    void Update()
    {
        ProgressTracker();
        Vector3 localTarget = ds.rb.gameObject.transform.InverseTransformPoint(target);
        Vector3 nextLocalTarget = ds.rb.gameObject.transform.InverseTransformPoint(nextTarget);

        float distanceToTarget = Vector3.Distance(target, ds.rb.gameObject.transform.position);

        float targetAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        float nextTargetAngle = Mathf.Atan2(nextLocalTarget.x, nextLocalTarget.z) * Mathf.Rad2Deg;

        float steer = Mathf.Clamp(targetAngle * steeringSensitivity, -1, 1) * Mathf.Sign(ds.currentSpeed);
        
        float distanceFactor = distanceToTarget / totalDistanceToTarget;
        float speedFactor = ds.currentSpeed / ds.maxSpeed;

        float accel = Mathf.Lerp(accelSensitivity, 1f, distanceFactor);
        float brake = Mathf.Lerp((-1 - Mathf.Abs(nextTargetAngle)) * brakingSensitivity, 1 + speedFactor, 1 - distanceFactor);

        if(Mathf.Abs(nextTargetAngle) > 20)
        {
            brake += 0.8f;
            accel -= 0.8f;
        }

        if (isJump)
        {
            accel = 1f;
            brake = 0f;
            Debug.Log("Jump");
        }

        //if(distanceToTarget < 5) { brake = 1.2f; accel = 0.1f; }
        Debug.Log("Brake: " + brake + " Accel:" + accel + " Speed: " + ds.rb.velocity.magnitude + " distanceFactor: " + distanceFactor);

        ds.Go(accel, steer, brake);

        if(distanceToTarget < 4) //threshold, make larger if car starts to circle waypoint
        {
            currentWP++;
            if(currentWP >= circuit.waypoints.Length)
                currentWP = 0;
            target = circuit.waypoints[currentWP].transform.position;
            if(currentWP == circuit.waypoints.Length - 1)
                nextTarget = circuit.waypoints[0].transform.position;
            else
                nextTarget = circuit.waypoints[currentWP + 1].transform.position;
            
            totalDistanceToTarget = Vector3.Distance(target, ds.rb.gameObject.transform.position);

            if(ds.rb.gameObject.transform.InverseTransformPoint(target).y > 5)
            {
                isJump = true;
            }
            else isJump = false;
        }

        ds.CheckForSkid();
        ds.CalculateEngineSound();
    }
}
