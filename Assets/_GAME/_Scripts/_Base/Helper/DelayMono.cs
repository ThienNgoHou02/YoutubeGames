using UnityEngine;
namespace DuongMike
{
    public class DelayMono : MonoBehaviour
    {
        public void Delay(float delayTime, System.Action action)
        {
            StartCoroutine(DelayCoroutine(delayTime, action));
        }
        private System.Collections.IEnumerator DelayCoroutine(float delayTime, System.Action action)
        {
            yield return WaitForSecondCache.Get(delayTime);
            action?.Invoke();
        }
    }

}
