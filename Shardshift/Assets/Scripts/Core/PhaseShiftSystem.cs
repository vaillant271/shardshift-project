using UnityEngine;

public class PhaseShiftSystem : MonoBehaviour
{
    public enum Phase { Materialis, Spectrus }
    public Phase CurrentPhase { get; private set; } = Phase.Materialis;

    public void TogglePhase()
    {
        CurrentPhase = CurrentPhase == Phase.Materialis ? Phase.Spectrus : Phase.Materialis;
        Debug.Log("Phase switched to: " + CurrentPhase);
    }
}