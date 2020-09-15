using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserPointer : MonoBehaviour {

    // privateフィールドをインスペクタに表示する際に付けるおまじない
    [SerializeField]
    private Transform _RightHandAnchor; // 右手
    [SerializeField]
    private Transform _LeftHandAnchor; // 左手
    [SerializeField]
    private Transform _CenterEyeAnchor; // 目の中心
    [SerializeField]
    private float _MaxDistance = 100.0f; // 距離
    [SerializeField]
    private LineRenderer _LaserPointerRenderer;

    // オブジェクトを掴む
    private GameObject _grabObject = null;
    private Vector3 _grabOffset;
    private float _grabDistance = 0;
    private Vector3 _grabPrevFramePos;

	// コントローラー
    private Transform Pointer{
        get{
            // 現在アクティブなコントローラーを取得
            var controller = OVRInput.GetActiveController();
            if(controller == OVRInput.Controller.RTrackedRemote){
                // 右手
                return _RightHandAnchor;
            } else if(controller == OVRInput.Controller.LTrackedRemote){
                // 左手
                return _LeftHandAnchor;
            }
            // どちらも取れなければ目の間からビームが出る
            return _CenterEyeAnchor;
        }
    }

    /// <summary>
    /// Goal : コントローラのレーザーカーソルを当てたオブジェクトを掴み、投げれるアプリを作る
    /// </summary>
	void Update () {
        var pointer = Pointer; // コントローラーを取得

        // コントローラーがない or LineRendererがなければなにもしない
        if(pointer == null || _LaserPointerRenderer == null){
            return;
        }
        // コントローラー位置からRayを飛ばす
        Ray pointerRay = new Ray(pointer.position, pointer.forward);
        // レーザーの起点
        _LaserPointerRenderer.SetPosition(0, pointerRay.origin);

        RaycastHit hitInfo;
        if(Physics.Raycast(pointerRay, out hitInfo, _MaxDistance)){
            // Rayがヒットしたらそこまで
            _LaserPointerRenderer.SetPosition(1, hitInfo.point);
            // ヒットしたオブジェクトを取得
            GameObject hitObj = hitInfo.collider.gameObject;                

            // トリガーを押している間の処理
            if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger)
                            && hitObj.tag == "Ball") {
                Debug.Log("トリガーボタン押下中...");

                _grabObject = hitObj;
                _grabDistance = hitInfo.distance;
                // ヒットした場所からオブジェクト中心までの距離
                _grabOffset = hitObj.transform.position - hitInfo.point;
                _grabPrevFramePos = hitObj.transform.position;

            // トリガーを離した時の処理
            }else if(OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger)){
                Vector3 force = _grabObject.transform.position - _grabPrevFramePos;
                _grabObject.GetComponent<Rigidbody>().velocity = force * 30f;
                _grabObject = null;
            
            // タッチパッドボタンを押した時
            } else if(OVRInput.GetDown(OVRInput.Button.PrimaryTouchpad)){
                Debug.Log("タッチパッドボタン押下");
                // TODO 移動
            }

            // 掴んだオブジェクトを移動
            if (_grabObject != null)
            {
                // 上下タッチで距離変更
                Vector2 pt = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad); // タッチパッドの位置
                if (pt.y > +0.6 && (-0.5 < pt.x && pt.x < +0.5))
                {
                    // 上部
                    _grabDistance += 0.1f;
                }
                else if (pt.y < -0.9 && (-0.5 < pt.x && pt.x < +0.5))
                {
                    // 下部
                    _grabDistance -= 0.1f;
                    if (_grabDistance < 0.1) _grabDistance = 0.1f;
                }
                // 移動
                _grabPrevFramePos = _grabObject.transform.position;
                _grabObject.transform.position = pointerRay.GetPoint(_grabDistance) + _grabOffset;
                _grabObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }

        } else {
            // Rayがヒットしなかったら向いている方向にMaxDistanceを伸ばす
            _LaserPointerRenderer
                .SetPosition(1, pointerRay.origin + pointerRay.direction * _MaxDistance);
        }
	}
}

/* Reference ****************************************************************************
 * ボタン変数
     トリガー：OVRInput.Button.PrimaryIndexTrigger
     タッチパッドのクリック：OVRInput.Button.PrimaryTouchpad
     タッチパッドの上方向スクロール：OVRInput.Button.Up
     タッチパッドの下方向スクロール：OVRInput.Button.down
     タッチパッドの左方向スクロール：OVRInput.Button.left
     タッチパッドの右方向スクロール：OVRInput.Button.right
     バックボタン：OVRInput.Button.Back

 * ボタン処理
     bool result = OVRInput.Get({ボタン変数名});     // ボタンが押されている間ずっとTrueを返す
     bool result = OVRInput.GetDown({ボタン変数名}); // ボタンが押された時の1回のみTrueを返す
     bool result = OVRInput.GetUp({ボタン変数名});   // ボタンが離された時の1回のみTrueを返す

 * タッチパッド上の2次元座標
    Vector2 vector =  OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad,
                                    OVRInput.Controller.RTrackedRemote);
    float x = vector.x;
    float y = vector.y;

 Ex)
 if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)){
     // トリガー押された時の処理
 }
 */