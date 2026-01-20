using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    [Header("Loading Settings")]
    [SerializeField] private string targetSceneName = "Home";
    [SerializeField] private float minimumLoadingTime = 1f; // Thời gian loading tối thiểu (giây)
    [SerializeField] private float loadingBarFillDuration = 2f; // Thời gian fill thanh loading (giây)
    
    [Header("UI References (Optional)")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Transform xoay;
    
    private void Start()
    {
        // Kiểm tra xem có scene target được set từ LevelSelection không
        string targetScene = PlayerPrefs.GetString("TargetScene", "");
        if (!string.IsNullOrEmpty(targetScene))
        {
            targetSceneName = targetScene;
            PlayerPrefs.DeleteKey("TargetScene"); // Clear sau khi đã dùng
        }
        
        StartCoroutine(LoadSceneAsync());
        xoay.DORotate(360f * Vector3.forward, 1f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.InOutSine);
    }
    
    private IEnumerator LoadSceneAsync()
    {
        float startTime = Time.time;
        
        // Bắt đầu load scene bất đồng bộ
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
        
        // Không tự động chuyển scene khi load xong (để có thể hiển thị 100% progress)
        operation.allowSceneActivation = false;
        
        // Fill thanh loading theo thời gian
        float fillElapsedTime = 0f;
        bool sceneLoaded = false;
        
        while (!sceneLoaded || fillElapsedTime < loadingBarFillDuration)
        {
            fillElapsedTime += Time.deltaTime;
            
            // Tính progress dựa trên thời gian (fill dần theo thời gian)
            float timeBasedProgress = Mathf.Clamp01(fillElapsedTime / loadingBarFillDuration);
            
            // Cập nhật progress bar với giá trị smooth
            if (progressBar != null)
            {
                progressBar.value = timeBasedProgress;
            }
            
            // Kiểm tra xem scene đã load xong chưa
            if (operation.progress >= 0.9f)
            {
                sceneLoaded = true;
            }
            
            yield return null;
        }
        
        // Đảm bảo thanh loading đã full 100%
        if (progressBar != null)
        {
            progressBar.value = 1f;
        }
        
        // Đảm bảo đã đủ thời gian loading tối thiểu
        float totalElapsedTime = Time.time - startTime;
        if (totalElapsedTime < minimumLoadingTime)
        {
            yield return new WaitForSeconds(minimumLoadingTime - totalElapsedTime);
        }
        
        // Kích hoạt scene
        operation.allowSceneActivation = true;
    }
}
