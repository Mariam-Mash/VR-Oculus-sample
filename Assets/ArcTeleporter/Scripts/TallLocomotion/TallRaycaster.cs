using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TallRaycaster : ArcRaycaster {
	[Tooltip("Minimum pitch affecting curve size and raycast distance")]
	public float minControllerAngle = 63;
	[Tooltip("Maximum pitch affecting curve size and raycast distance")]
	public float maxControllerAngle = 120;
	[Tooltip("Maximum distance to raycast to")]
	public float maxDistance = 12;
	[Tooltip("Minimum distance to raycast to")]
	public float minDistance = 0.5f;
	[Tooltip("How far above the user the cast should start")]
	public float castHeight = 2;
	[Tooltip("How far below the \"ground\" the cast should continue, a precentage of castHeight")]
	public float dropMultiplyer = 1.0f;
	[Tooltip("Length added to cast ray, helps target ground layers that are far below the player")]
	public float extraCastLength = 10.0f;

    // オブジェクトを掴む
    private GameObject _grabObject = null;
    private Vector3 _grabOffset;
    private float _grabDistance = 0;
    private Vector3 _grabPrevFramePos;

    void Awake() {
		if (trackingSpace == null && OVRManager.instance != null) {
			GameObject cameraObject = OVRManager.instance.gameObject;
			trackingSpace = cameraObject.transform.Find ("TrackingSpace");
			Debug.LogWarning ("Tracking space not set for TallRaycaster");
		}
		if (trackingSpace == null) {
			Debug.LogError ("Tracking MUST BE set for TallRaycaster");
		}
	}

	public float MaxDistnce {
		get {
			return maxDistance;
		}
	}

	Ray HorizontalRay {
		get {
			Vector3 horizontalDirection = Vector3.ProjectOnPlane (ControllerForward, Up);
			Vector3 playerPosition = trackingSpace.position;
			return new Ray (playerPosition, horizontalDirection);
		}
	}

	public float HorizontalDistance {
		get {
			float controllerAngle = Vector3.Angle(Up * -1.0f, ControllerForward);
			float pitch = Mathf.Clamp(controllerAngle, minControllerAngle, maxControllerAngle);

			float distanceRange = maxDistance - minDistance;
			float pitchRange = maxControllerAngle - minControllerAngle;

			float t = (pitch - minControllerAngle) / pitchRange;
			return minDistance + distanceRange * t;
		}
	}

	Vector3 PointAlongHorizontalRay {
		get {
			return HorizontalRay.origin + HorizontalRay.direction * HorizontalDistance;
		}
	}

	Vector3 CastPosition {
		get {
			Vector3 playerPosition = trackingSpace.position;
			return playerPosition + Up * castHeight;
		}
	}

	Ray CastRay {
		get {
			Vector3 castDirection = (PointAlongHorizontalRay - CastPosition).normalized;

			return new Ray (CastPosition, castDirection);
		}
	}

	void Update() {
		MakingContact = false;
		HitPoint = PointAlongHorizontalRay + (PointAlongHorizontalRay - CastRay.origin) * dropMultiplyer;

		float rayLength = (CastPosition - HitPoint).magnitude + extraCastLength; 

		#if UNITY_EDITOR
		Debug.DrawLine(CastRay.origin, CastRay.origin + CastRay.direction * rayLength, Color.red);
		Debug.DrawLine(HorizontalRay.origin, HorizontalRay.origin + HorizontalRay.direction * rayLength, Color.blue);
		#endif

		RaycastHit hit;
		if (Physics.Raycast(CastRay, out hit, rayLength, ~excludeLayers)) {
			float angle = Vector3.Angle(Vector3.up, hit.normal);
			if (angle < surfaceAngle) {
				HitPoint = hit.point;
				Normal = hit.normal;
				MakingContact = true;
			}
            // タッチパッドをクリックした時の挙動を記入
            // Catch & Release *************************************************
            // ヒットしたオブジェクトを取得
            GameObject hitObj = hit.collider.gameObject;
            if (OVRInput.GetDown(OVRInput.Button.PrimaryTouchpad)
                            && hitObj.tag == "Ball"){

                _grabObject = hitObj;
                _grabDistance = HorizontalDistance;
                // ヒットした場所からオブジェクト中心までの距離
                _grabOffset = hitObj.transform.position - hit.point;
                _grabPrevFramePos = hitObj.transform.position;

            } else if (OVRInput.GetUp(OVRInput.Button.PrimaryTouchpad)){
                Vector3 force = _grabObject.transform.position - _grabPrevFramePos;
                _grabObject.GetComponent<Rigidbody>().velocity = force * 30f;
                _grabObject = null;

            }

            // 掴んだオブジェクトを移動
            if (_grabObject != null) {
                // 上下タッチで距離変更
                Vector2 pt = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad); // タッチパッドの位置
                if (pt.y > +0.6 && (-0.5 < pt.x && pt.x < +0.5)) {
                    // 上部
                    _grabDistance += 0.1f;
                } else if (pt.y < -0.9 && (-0.5 < pt.x && pt.x < +0.5)) {
                    // 下部
                    _grabDistance -= 0.1f;
                    if (_grabDistance < 0.1) _grabDistance = 0.1f;
                }
                // 移動
                _grabPrevFramePos = _grabObject.transform.position;
                _grabObject.transform.position = HorizontalRay.GetPoint(_grabDistance) + _grabOffset;
                _grabObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }

        }
	}
}