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
        
        // Cập nhật UI trong khi loading
        while (!operation.isDone)
        {
            // Progress từ 0 đến 0.9 khi đang load
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // Cập nhật progress bar
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            
            // Cập nhật percentage text
            
            // Khi đã load xong (progress >= 0.9) và đã đủ thời gian tối thiểu
            if (operation.progress >= 0.9f)
            {
                float elapsedTime = Time.time - startTime;
                
                // Đảm bảo hiển thị 100% trước khi chuyển scene
                if (progressBar != null)
                {
                    progressBar.value = 1f;
                }
                
                // Chờ đến khi đủ thời gian tối thiểu
                if (elapsedTime >= minimumLoadingTime)
                {
                    // Kích hoạt scene
                    operation.allowSceneActivation = true;
                }
                else
                {
                    // Chờ thêm thời gian còn lại
                    yield return new WaitForSeconds(minimumLoadingTime - elapsedTime);
                    operation.allowSceneActivation = true;
                }
            }
            
            yield return null;
        }
    }
}
