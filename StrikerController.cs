using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;

public class StrikerController : MonoBehaviour
{
    [SerializeField] private Slider strikerSlider;
    [SerializeField] private LineRenderer trajectoryLineRenderer;
    [SerializeField] private LineRenderer coinTrajectoryLineRenderer;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private GameObject velocityInputBox;
    [SerializeField] private InputField velocityInput;
    [SerializeField] private LineRenderer wallTrajectoryLineRenderer;

    private Rigidbody2D rb;

    private Vector2 dragStart;
    private Vector2 dragEnd;
    private bool isDragging = false;

    public float correctVelocity;  // Correct velocity to sink the coin
    public float velocityMargin = 1.0f;  // Acceptable margin of error

    public GameObject coin;  // Reference to the coin
    public Transform hole;  // Reference to the hole position
    public LayerMask collisionLayerMask;

    private bool strikerPlaced = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (strikerSlider != null)
        {
            strikerSlider.onValueChanged.AddListener(StrikerXPos);
        }

        ConfigureTrajectoryLine(trajectoryLineRenderer);
        ConfigureTrajectoryLine(coinTrajectoryLineRenderer);

        velocityInputBox.SetActive(false);

        CalculateCorrectVelocity();
    }

    void Update()
    {
        if (!strikerPlaced) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Vector2.Distance(mousePosition, transform.position) < 0.5f)
            {
                dragStart = mousePosition;
                isDragging = true;
                trajectoryLineRenderer.enabled = true;
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 currentDragPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            UpdateAimingVisuals(dragStart, currentDragPosition);
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            dragEnd = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            isDragging = false;

            trajectoryLineRenderer.enabled = false;
            coinTrajectoryLineRenderer.enabled = false;

            ShowVelocityInputBox();
        }
    }

    public void StrikerXPos(float value)
    {
        transform.position = new Vector3(value, -1.9f, 0);
        strikerPlaced = true;
    }

    private void CalculateCorrectVelocity()
    {
        // Calculate the distance between the coin and the hole
        float distanceToHole = Vector2.Distance(coin.transform.position, hole.position);

        // Use a physics-based formula for the correct velocity
        correctVelocity = Mathf.Sqrt(2 * distanceToHole * Physics2D.gravity.magnitude);

        Debug.Log($"Correct Velocity: {correctVelocity}");
    }

    private void ShowVelocityInputBox()
    {
        velocityInputBox.SetActive(true);
    }

    public void OnSubmitVelocity()
    {
        if (float.TryParse(velocityInput.text, out float enteredVelocity))
        {
            velocityInputBox.SetActive(false);

            if (enteredVelocity > 10.88f)
            {
                Debug.Log("Too fast");
            }
            else if (enteredVelocity < 10.88f)
            {
                Debug.Log("Too slow");
            }

            ApplyForce(enteredVelocity); // Apply force regardless, and physics will determine what happens
        }
        else
        {
            Debug.LogError("Invalid velocity input!");
        }
    }


    private void UpdateAimingVisuals(Vector2 start, Vector2 end)
    {
        // Direction should be from dragEnd to dragStart (since the striker is pulled back)
        Vector2 direction = (start - end).normalized;
        float dragDistance = Vector2.Distance(start, end);
        DrawTrajectoryLine(transform.position, direction, dragDistance);
        DrawCoinTrajectory(direction); // Pass the correct direction
    }

    private void DrawTrajectoryLine(Vector2 startPosition, Vector2 direction, float length)
    {
        trajectoryLineRenderer.SetPosition(0, startPosition);
        trajectoryLineRenderer.SetPosition(1, startPosition + direction * length);
    }

    private void DrawCoinTrajectory(Vector2 strikerDirection)
    {
        // Clear previous trajectory
        coinTrajectoryLineRenderer.positionCount = 0;

        RaycastHit2D strikerHit = Physics2D.Raycast(transform.position, strikerDirection, Mathf.Infinity, collisionLayerMask);

        if (strikerHit.collider != null && strikerHit.collider.CompareTag("Coin"))
        {
            Vector2 hitPoint = strikerHit.point; // Where the striker hits the coin
            Vector2 coinCenter = strikerHit.collider.transform.position;

            // Calculate the angle of the striker to the coin with the horizontal
            float strikerToCoinAngle = 180-(Mathf.Atan2(coinCenter.y - transform.position.y, coinCenter.x - transform.position.x) * Mathf.Rad2Deg);

            // Calculate the angle of the coin to the hole with the horizontal
            float coinToHoleAngle = 180-(Mathf.Atan2(hole.position.y - coinCenter.y, hole.position.x - coinCenter.x) * Mathf.Rad2Deg);


            // Calculate the distance between the striker and the coin
            // Convert and display striker-to-coin distance
            float strikerToCoinDistance = ConvertToRealWorldDistance(Vector2.Distance(transform.position, coinCenter));
            Debug.Log($"Distance between Striker and Coin: {strikerToCoinDistance}");

            // Display the distance on the UI
            if (distanceText != null)
            {
                distanceText.text = $"Striker to Coin: {strikerToCoinDistance:F2} meters\n";
                distanceText.text += $"Angle (Striker → Coin): {strikerToCoinAngle:F2}°\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n";
            }

            // Calculate the normal at the point of impact (green ray)
            Vector2 normal = (hitPoint - coinCenter).normalized;

            // Calculate the direction of the coin's movement after being hit (blue ray)
            Vector2 coinDirection = -normal; // Exact opposite of the green ray

            // Debug logs to verify directions
            Debug.Log($"Striker Direction: {strikerDirection}");
            Debug.Log($"Normal (Green Ray): {normal}");
            Debug.Log($"Coin Direction (Blue Ray): {coinDirection}");

            // Draw debug rays to visualize the directions
            Debug.DrawRay(hitPoint, strikerDirection, Color.red, 2f); // Striker direction (red)
            Debug.DrawRay(hitPoint, normal, Color.green, 2f); // Normal (green)
            Debug.DrawRay(hitPoint, coinDirection, Color.blue, 2f); // Coin's movement direction (blue)

            // Move start point slightly beyond the coin's edge
            float coinRadius = strikerHit.collider.bounds.extents.x; // Approximate radius
            Vector2 startFuturePath = hitPoint + coinDirection * (coinRadius + 0.1f); // Start just outside the coin

            // Temporarily disable the coin's collider to avoid self-collision
            Collider2D coinCollider = strikerHit.collider;
            coinCollider.enabled = false;

            List<Vector2> trajectoryPoints = new List<Vector2>();
            trajectoryPoints.Add(startFuturePath); // First point (just outside the coin)
            int maxBounces = 3; // Limit bounces
            for (int i = 0; i < maxBounces; i++)
            {
                RaycastHit2D coinHit = Physics2D.Raycast(startFuturePath, coinDirection, 6f, collisionLayerMask);

                if (coinHit.collider != null)
                {
                    Vector2 bouncePoint = coinHit.point;
                    trajectoryPoints.Add(bouncePoint); // Add bounce point

                    // Check if the coin's future path hits the hole
                    if (coinHit.collider.CompareTag("Hole"))
                    {
                        // Convert and display coin-to-hole distance
                        float coinToHoleDistance = ConvertToRealWorldDistance(Vector2.Distance(coinCenter, hole.position));
                        Debug.Log($"Distance between Coin and Hole: {coinToHoleDistance}");

                        if (distanceText != null)
                        {
                            distanceText.text += $"\n\n\n\n\n\nCoin to Hole: {coinToHoleDistance:F2} meters\n";
                            distanceText.text += $"Angle (Coin → Hole): {coinToHoleAngle:F2}°";
                        }

                        // Stop extending the trajectory since it reached the hole
                        break;
                    }

                    // Reflect the direction correctly
                    coinDirection = Vector2.Reflect(coinDirection, coinHit.normal);
                    startFuturePath = bouncePoint + coinDirection * 0.1f; // Small offset to avoid self-collision
                }
                else
                {
                    trajectoryPoints.Add(startFuturePath + coinDirection * 6f); // Extend path
                    break;
                }
            }


            // Re-enable the coin's collider
            coinCollider.enabled = true;

            coinTrajectoryLineRenderer.enabled = true;

            // Update LineRenderer with calculated points
            coinTrajectoryLineRenderer.positionCount = trajectoryPoints.Count;
            for (int i = 0; i < trajectoryPoints.Count; i++)
            {
                coinTrajectoryLineRenderer.SetPosition(i, trajectoryPoints[i]);
            }
        }
        else
        {
            coinTrajectoryLineRenderer.enabled = false;
        }
    }




    private void ApplyForce(float enteredVelocity)
    {
        Vector2 direction = (dragStart - dragEnd).normalized;
        rb.velocity = direction * enteredVelocity;

        Debug.Log("Striker launched with velocity: " + enteredVelocity);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Hole"))
        {
            Rigidbody2D coinRb = GetComponent<Rigidbody2D>();

            if (coinRb.velocity.magnitude > 10.88f) // Too fast to sink
            {
                Debug.Log("Too fast");
                coinRb.velocity = coinRb.velocity.normalized * 5f; // Make it move past the hole
            }
            else if (coinRb.velocity.magnitude < 10.88f) // Too slow to reach the hole
            {
                Debug.Log("Too slow");
                coinRb.velocity = Vector2.zero; // Stop the coin before reaching the hole
            }
            else
            {
                Debug.Log("Coin Sunk!");
                // Instead of destroying, move it inside the hole and stop
                coinRb.velocity = Vector2.zero;
                coinRb.isKinematic = true; // Disable physics interactions
                transform.position = other.transform.position; // Place exactly in the hole
            }
        }
    }




    private void ConfigureTrajectoryLine(LineRenderer lineRenderer)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }


    


        private float ConvertToRealWorldDistance(float unityDistance)
    {
        // Define a conversion factor (Adjust based on your game's scale)
        float conversionFactor = 0.1f; // Example: 1 Unity unit = 0.1 meters

        return unityDistance * conversionFactor;
    }


}
