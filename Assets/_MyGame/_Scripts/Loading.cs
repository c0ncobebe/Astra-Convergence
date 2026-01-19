using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Text loadingText;
    [SerializeField] private Text percentageText;
    [SerializeField] private List<Vector2> dadssad;
    
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
            if (percentageText != null)
            {
                percentageText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
            
            // Cập nhật loading text
            if (loadingText != null)
            {
                int dotCount = Mathf.FloorToInt(Time.time * 2) % 4;
                loadingText.text = "Loading" + new string('.', dotCount);
            }
            
            // Khi đã load xong (progress >= 0.9) và đã đủ thời gian tối thiểu
            if (operation.progress >= 0.9f)
            {
                float elapsedTime = Time.time - startTime;
                
                // Đảm bảo hiển thị 100% trước khi chuyển scene
                if (progressBar != null)
                {
                    progressBar.value = 1f;
                }
                if (percentageText != null)
                {
                    percentageText.text = "100%";
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
