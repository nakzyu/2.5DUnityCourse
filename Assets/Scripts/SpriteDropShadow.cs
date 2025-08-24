using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDropShadow : MonoBehaviour
{
    [Header("그림자 설정")]
    [SerializeField] private bool createShadowOnStart = true;
    [SerializeField] private Vector2 shadowOffset = new Vector2(0.1f, -0.1f);
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] private int shadowSortingOrder = -1;

    [Header("그림자 효과")]
    [SerializeField] private bool usePerspective = false;
    [SerializeField] private float perspectiveScale = 0.8f;
    [SerializeField] private bool followParentRotation = true;

    private GameObject shadowObject;
    private SpriteRenderer originalSpriteRenderer;
    private SpriteRenderer shadowSpriteRenderer;

    void Start()
    {
        if (createShadowOnStart)
        {
            CreateShadow();
        }
    }

    void Update()
    {
        if (shadowObject != null)
        {
            UpdateShadowPosition();
            UpdateShadowAppearance();
        }
    }

    public void CreateShadow()
    {
        if (shadowObject != null)
        {
            DestroyImmediate(shadowObject);
        }

        // 원본 스프라이트 렌더러 가져오기
        originalSpriteRenderer = GetComponent<SpriteRenderer>();

        // 그림자 오브젝트 생성
        shadowObject = new GameObject(gameObject.name + "_Shadow");
        shadowObject.transform.parent = transform;

        // 그림자 스프라이트 렌더러 설정
        shadowSpriteRenderer = shadowObject.AddComponent<SpriteRenderer>();
        shadowSpriteRenderer.sprite = originalSpriteRenderer.sprite;
        shadowSpriteRenderer.color = shadowColor;

        // 정렬 순서 설정
        shadowSpriteRenderer.sortingLayerName = originalSpriteRenderer.sortingLayerName;
        shadowSpriteRenderer.sortingOrder = originalSpriteRenderer.sortingOrder + shadowSortingOrder;

        UpdateShadowPosition();
    }

    private void UpdateShadowPosition()
    {
        if (shadowObject == null) return;

        // 그림자 위치 업데이트
        shadowObject.transform.localPosition = shadowOffset;

        // 원근감 효과
        if (usePerspective)
        {
            shadowObject.transform.localScale = Vector3.one * perspectiveScale;
        }
        else
        {
            shadowObject.transform.localScale = Vector3.one;
        }

        // 회전 처리
        if (!followParentRotation)
        {
            shadowObject.transform.rotation = Quaternion.identity;
        }
        else
        {
            shadowObject.transform.localRotation = Quaternion.identity;
        }
    }

    private void UpdateShadowAppearance()
    {
        if (shadowSpriteRenderer == null || originalSpriteRenderer == null) return;

        // 원본 스프라이트가 변경되었을 때 그림자도 업데이트
        if (shadowSpriteRenderer.sprite != originalSpriteRenderer.sprite)
        {
            shadowSpriteRenderer.sprite = originalSpriteRenderer.sprite;
        }

        // 원본이 비활성화되면 그림자도 비활성화
        shadowSpriteRenderer.enabled = originalSpriteRenderer.enabled;

        // 플립 상태 동기화
        shadowSpriteRenderer.flipX = originalSpriteRenderer.flipX;
        shadowSpriteRenderer.flipY = originalSpriteRenderer.flipY;
    }

    public void SetShadowOffset(Vector2 offset)
    {
        shadowOffset = offset;
        UpdateShadowPosition();
    }

    public void SetShadowColor(Color color)
    {
        shadowColor = color;
        if (shadowSpriteRenderer != null)
        {
            shadowSpriteRenderer.color = shadowColor;
        }
    }

    public void SetShadowScale(float scale)
    {
        perspectiveScale = scale;
        UpdateShadowPosition();
    }

    public void ToggleShadow(bool enabled)
    {
        if (shadowObject != null)
        {
            shadowObject.SetActive(enabled);
        }
    }

    public void RemoveShadow()
    {
        if (shadowObject != null)
        {
            DestroyImmediate(shadowObject);
            shadowObject = null;
            shadowSpriteRenderer = null;
        }
    }

    void OnDestroy()
    {
        RemoveShadow();
    }

    // 에디터에서 값 변경 시 실시간 업데이트
#if UNITY_EDITOR
    void OnValidate()
    {
        if (shadowObject != null && Application.isPlaying)
        {
            UpdateShadowPosition();
            SetShadowColor(shadowColor);
        }
    }
#endif
}

// 고급 그림자 시스템 (블러 효과 포함)
public class AdvancedShadow2D : MonoBehaviour
{
    [Header("고급 그림자 설정")]
    [SerializeField] private bool enableDynamicShadow = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxShadowDistance = 5f;
    [SerializeField] private AnimationCurve shadowFalloff = AnimationCurve.Linear(0, 1, 1, 0);

    [Header("그림자 투사")]
    [SerializeField] private Vector2 lightDirection = new Vector2(1, -1).normalized;
    [SerializeField] private float shadowLength = 2f;
    [SerializeField] private bool autoDetectGround = true;

    private SpriteRenderer spriteRenderer;
    private GameObject shadowContainer;
    private SpriteRenderer[] shadowLayers;
    private int shadowLayerCount = 3;

    void Start()
    {
        InitializeShadowSystem();
    }

    void InitializeShadowSystem()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 그림자 컨테이너 생성
        shadowContainer = new GameObject("ShadowContainer");
        shadowContainer.transform.parent = transform;
        shadowContainer.transform.localPosition = Vector3.zero;

        // 다중 레이어 그림자 생성 (블러 효과)
        shadowLayers = new SpriteRenderer[shadowLayerCount];

        for (int i = 0; i < shadowLayerCount; i++)
        {
            GameObject shadowLayer = new GameObject($"ShadowLayer_{i}");
            shadowLayer.transform.parent = shadowContainer.transform;

            SpriteRenderer sr = shadowLayer.AddComponent<SpriteRenderer>();
            sr.sprite = spriteRenderer.sprite;

            // 레이어별로 투명도와 크기 조정
            float alpha = 0.3f / (i + 1);
            sr.color = new Color(0, 0, 0, alpha);

            float scale = 1f + (i * 0.05f);
            shadowLayer.transform.localScale = Vector3.one * scale;

            sr.sortingLayerName = spriteRenderer.sortingLayerName;
            sr.sortingOrder = spriteRenderer.sortingOrder - (shadowLayerCount - i);

            shadowLayers[i] = sr;
        }
    }

    void Update()
    {
        if (!enableDynamicShadow || shadowContainer == null) return;

        UpdateDynamicShadow();
    }

    void UpdateDynamicShadow()
    {
        float groundDistance = GetGroundDistance();

        // 지면과의 거리에 따른 그림자 페이드
        float shadowAlpha = shadowFalloff.Evaluate(groundDistance / maxShadowDistance);

        for (int i = 0; i < shadowLayers.Length; i++)
        {
            if (shadowLayers[i] == null) continue;

            // 각 레이어 위치 업데이트
            Vector3 offset = (Vector3)(lightDirection * shadowLength * (1f - groundDistance / maxShadowDistance));
            offset.z = 0;
            offset *= (1f + i * 0.1f); // 레이어별 오프셋 차이

            shadowLayers[i].transform.localPosition = offset;

            // 투명도 업데이트
            Color currentColor = shadowLayers[i].color;
            currentColor.a = shadowAlpha * (0.3f / (i + 1));
            shadowLayers[i].color = currentColor;

            // 스프라이트 동기화
            shadowLayers[i].sprite = spriteRenderer.sprite;
            shadowLayers[i].flipX = spriteRenderer.flipX;
            shadowLayers[i].flipY = spriteRenderer.flipY;
        }
    }

    float GetGroundDistance()
    {
        if (!autoDetectGround) return 0;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, maxShadowDistance, groundLayer);

        if (hit.collider != null)
        {
            return hit.distance;
        }

        return maxShadowDistance;
    }

    public void SetLightDirection(Vector2 direction)
    {
        lightDirection = direction.normalized;
    }

    public void SetShadowIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);

        for (int i = 0; i < shadowLayers.Length; i++)
        {
            if (shadowLayers[i] != null)
            {
                Color color = shadowLayers[i].color;
                color.a = intensity * (0.3f / (i + 1));
                shadowLayers[i].color = color;
            }
        }
    }

    void OnDestroy()
    {
        if (shadowContainer != null)
        {
            DestroyImmediate(shadowContainer);
        }
    }
}