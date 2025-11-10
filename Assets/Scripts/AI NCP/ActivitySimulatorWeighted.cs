using System.Collections.Generic;
using UnityEngine;

public class ActivitySimulatorWeighted : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Total real-life minutes to simulate 24 hours")]
    public float minutesPerDay = 10f;  // Example: 10 minutes = 24 hours

    private float elapsedMinutes = 0f;
    private int currentHour = -1;  // Track last updated hour

    [Header("Weighted Activities per Hour")]
    private Dictionary<int, List<(string activity, float probability)>> hourlyActivities =
        new Dictionary<int, List<(string, float)>>()
    {
        {0, new List<(string,float)>{("Sleep",0.80f), ("Phone",0.10f), ("WatchTV",0.05f), ("Study",0.03f), ("Work",0.02f)}},
        {1, new List<(string,float)>{("Sleep",0.9f), ("Chat",0.05f), ("WatchTV",0.03f), ("Game",0.02f)}},
        {2, new List<(string,float)>{("Sleep",0.9f), ("Work",0.05f), ("WatchTV",0.03f), ("Game",0.02f)}},
        {3, new List<(string,float)>{("Sleep",0.70f), ("Work",0.10f), ("Drive",0.05f), ("Study",0.10f), ("WatchTV",0.05f)}},
        {4, new List<(string,float)>{("Sleep",0.60f), ("Wake",0.15f), ("FinishShift",0.10f), ("Pray",0.15f)}},
        {5, new List<(string,float)>{("Wake",0.50f), ("Pray",0.20f), ("Exercise",0.15f), ("Breakfast",0.15f)}},
        {6, new List<(string,float)>{("Wake",0.50f), ("Brush",0.10f), ("Walk",0.20f), ("Breakfast",0.20f)}},
        {7, new List<(string,float)>{("Wake",0.40f), ("Breakfast",0.30f), ("Exercise",0.15f), ("GetReady",0.15f)}},
        {8, new List<(string,float)>{("Breakfast",0.30f), ("Commute",0.40f), ("Work",0.20f), ("Meeting",0.10f)}},
        {9, new List<(string,float)>{("Work",0.50f), ("Class",0.20f), ("Meeting",0.20f), ("Study",0.10f)}},
        {10,new List<(string,float)>{("Work",0.50f), ("Meeting",0.20f), ("Study",0.20f), ("Gym",0.10f)}},
        {11,new List<(string,float)>{("Work",0.40f), ("Meeting",0.20f), ("LunchPrep",0.20f), ("Shop",0.20f)}},
        {12,new List<(string,float)>{("Lunch",0.60f), ("Break",0.15f), ("Rest",0.15f), ("WrapWork",0.10f)}},
        {13,new List<(string,float)>{("Lunch",0.50f), ("Work",0.30f), ("Nap",0.10f), ("WatchTV",0.10f)}},
        {14,new List<(string,float)>{("Work",0.60f), ("Study",0.20f), ("Nap",0.10f), ("Chores",0.10f)}},
        {15,new List<(string,float)>{("Work",0.50f), ("Tea",0.20f), ("PickupKids",0.20f), ("Shop",0.10f)}},
        {16,new List<(string,float)>{("FinishWork",0.40f), ("Tea",0.20f), ("Play",0.20f), ("Exercise",0.20f)}},
        {17,new List<(string,float)>{("Walk",0.30f), ("ReturnHome",0.30f), ("Play",0.20f), ("Snack",0.20f)}},
        {18,new List<(string,float)>{("Home",0.30f), ("Relax",0.30f), ("Exercise",0.20f), ("Snack",0.20f)}},
        {19,new List<(string,float)>{("Dinner",0.50f), ("WatchTV",0.20f), ("Relax",0.20f), ("Chat",0.10f)}},
        {20,new List<(string,float)>{("Dinner",0.40f), ("Movie",0.30f), ("Talk",0.20f), ("Study",0.10f)}},
        {21,new List<(string,float)>{("DinnerEnd",0.40f), ("WatchTV",0.30f), ("Chat",0.20f), ("Study",0.10f)}},
        {22,new List<(string,float)>{("SleepPrep",0.50f), ("WatchTV",0.20f), ("Read",0.20f), ("Work",0.10f)}},
        {23,new List<(string,float)>{("Sleep",0.70f), ("Phone",0.10f), ("WatchTV",0.10f), ("Work",0.10f)}}
    };

    private string currentActivity;

    void Update()
    {
        // Advance simulated time
        float deltaMinutes = Time.deltaTime / 60f * (24f * 60f / minutesPerDay);
        elapsedMinutes += deltaMinutes;

        int newHour = Mathf.FloorToInt((elapsedMinutes / 60f) % 24);

        // Only update activity if the hour has changed
        if (newHour != currentHour)
        {
            currentHour = newHour;
            currentActivity = GetWeightedRandomActivity(hourlyActivities[currentHour]);

            // Debug log
            Debug.Log($"{gameObject.name} at hour {currentHour}: {currentActivity}");
        }
    }

    private string GetWeightedRandomActivity(List<(string activity, float probability)> activities)
    {
        float total = 0f;
        foreach (var a in activities)
            total += a.probability;

        float randomPoint = Random.value * total;

        foreach (var a in activities)
        {
            if (randomPoint < a.probability)
                return a.activity;
            else
                randomPoint -= a.probability;
        }

        return activities[activities.Count - 1].activity;
    }
}
