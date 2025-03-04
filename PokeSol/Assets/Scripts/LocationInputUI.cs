using UnityEngine;
using TMPro;

public class LocationInputUI : MonoBehaviour
{
    public SimulatedLocationProvider locationProvider;
    public TMP_InputField latitudeInput;
    public TMP_InputField longitudeInput;
    public TMP_InputField walletAddressInput; // Added this line
    public TextMeshProUGUI feedbackText;

    private float initialLatitude = 40.7128f;
    private float initialLongitude = -74.0060f;
    private float moveIncrement = 0.001f;

    void Start()
    {
        if (locationProvider != null)
        {
            latitudeInput.text = locationProvider.Latitude.ToString();
            longitudeInput.text = locationProvider.Longitude.ToString();
        }
    }

    public void UpdateLocation()
{
    Debug.Log($"Latitude Input: {latitudeInput.text}, Longitude Input: {longitudeInput.text}");
    if (float.TryParse(latitudeInput.text, out float lat) && float.TryParse(longitudeInput.text, out float lon))
    {
        Debug.Log($"Parsed Latitude: {lat}, Parsed Longitude: {lon}");
        locationProvider.SetLocation(lat, lon);
        feedbackText.text = $"Location set to: ({lat}, {lon})";
        Debug.Log($"Location set to: ({lat}, {lon})");
    }
    else
    {
        feedbackText.text = "Invalid latitude or longitude input!";
        Debug.LogWarning("Invalid latitude or longitude input!");
    }
}

    public void ResetLocation()
    {
        locationProvider.SetLocation(initialLatitude, initialLongitude);
        latitudeInput.text = initialLatitude.ToString();
        longitudeInput.text = initialLongitude.ToString();
        feedbackText.text = $"Location reset to: ({initialLatitude}, {initialLongitude})";
        Debug.Log($"Location reset to: ({initialLatitude}, {initialLongitude})");
    }

    public void MoveNorth()
    {
        float newLat = locationProvider.Latitude + moveIncrement;
        float newLon = locationProvider.Longitude;
        locationProvider.SetLocation(newLat, newLon);
        latitudeInput.text = newLat.ToString();
        longitudeInput.text = newLon.ToString();
        feedbackText.text = $"Moved North to: ({newLat}, {newLon})";
        Debug.Log($"Moved North to: ({newLat}, {newLon})");
    }

    public void MoveSouth()
    {
        float newLat = locationProvider.Latitude - moveIncrement;
        float newLon = locationProvider.Longitude;
        locationProvider.SetLocation(newLat, newLon);
        latitudeInput.text = newLat.ToString();
        longitudeInput.text = newLon.ToString();
        feedbackText.text = $"Moved South to: ({newLat}, {newLon})";
        Debug.Log($"Moved South to: ({newLat}, {newLon})");
    }

    public void MoveEast()
    {
        float newLat = locationProvider.Latitude;
        float newLon = locationProvider.Longitude + moveIncrement;
        locationProvider.SetLocation(newLat, newLon);
        latitudeInput.text = newLat.ToString();
        longitudeInput.text = newLon.ToString();
        feedbackText.text = $"Moved East to: ({newLat}, {newLon})";
        Debug.Log($"Moved East to: ({newLat}, {newLon})");
    }
    public void ClearWalletAddressInput()
{
    if (walletAddressInput.text == "Enter your wallet address:")
    {
        walletAddressInput.text = "";
    }
}

    public void MoveWest()
    {
        float newLat = locationProvider.Latitude;
        float newLon = locationProvider.Longitude - moveIncrement;
        locationProvider.SetLocation(newLat, newLon);
        latitudeInput.text = newLat.ToString();
        longitudeInput.text = newLon.ToString();
        feedbackText.text = $"Moved West to: ({newLat}, {newLon})";
        Debug.Log($"Moved West to: ({newLat}, {newLon})");
    }

    public void ClearWalletAddressInput(string walletAddress)
    {
        if (walletAddress == "Enter your wallet address:")
        {
            walletAddressInput.text = "";
        }
    }
}