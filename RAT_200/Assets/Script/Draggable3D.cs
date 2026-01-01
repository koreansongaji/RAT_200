using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class Draggable3D : MonoBehaviour
{
    [Header("When is drag allowed?")]
    public bool requireMicro = false;     // Micro������ �������
    public bool requireModifier = false;  // (����) ����Ű(Shift) �䱸

    public enum PlaneMode { CameraFacing, WorldUp, Custom, ScreenDepth }

    [Header("Plane & Axis")]
    public PlaneMode planeMode = PlaneMode.CameraFacing;
    public Transform customPlane;         // planeMode=Custom�� �� ����(��: up ���)
    public bool lockX, lockY, lockZ;     // ���
    public Vector3 snapStep = Vector3.zero; // (0�̸� ���� ����)
    float _grabDepthZ;           // ScreenDepth��: ī�޶�-���� ����
    Vector3 _grabPointWorld;     // ó�� ���� ����(�ɼ�)

    [Header("Bounds (optional)")]
    public Collider bounds;               // ��� �ݶ��̴�(���̸� ���� ����� ������ Ŭ����)

    public event Action OnDragStarted;
    public event Action OnDragEnded;

    Camera _cam;
    Plane _dragPlane;
    Vector3 _grabOffsetWorld;
    bool _dragging;

    [Header("Audio (optional)")]
    [SerializeField] AudioClip _slideSound;
    
    void Awake()
    {
        _cam = Camera.main;
    }

    public bool CanBeginDrag(Func<bool> inMicroCheck, Func<bool> modifierCheck)
    {
        if (requireMicro && (inMicroCheck == null || !inMicroCheck())) return false;
        if (requireModifier && (modifierCheck == null || !modifierCheck())) return false;
        return true;
    }

    public void BeginDrag(Ray pointerRay)
    {
        if(_slideSound != null)
            AudioManager.Instance.Play(_slideSound, AudioManager.Sound.Effect, Random.Range(0.9f, 1.1f));
        
        _dragging = true;

        if (planeMode == PlaneMode.WorldUp)
        {
            // ����(XZ) ���
            _dragPlane = new Plane(Vector3.up, transform.position);
        }
        else if (planeMode == PlaneMode.Custom && customPlane != null)
        {
            _dragPlane = new Plane(customPlane.up, customPlane.position);
        }
        else if (planeMode == PlaneMode.ScreenDepth)
        {
            // ���� ������Ʈ�� ���̸� ����
            _grabPointWorld = transform.position;
            var sp = _cam.WorldToScreenPoint(_grabPointWorld);
            _grabDepthZ = sp.z;

            // ȭ������ ��ȯ ���������� ������ ���
            var mouse = UnityEngine.Input.mousePosition;
            var worldAtDepth = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _grabDepthZ));
            _grabOffsetWorld = transform.position - worldAtDepth;
            OnDragStarted?.Invoke();
            return; // ScreenDepth�� ��� ����ĳ��Ʈ�� �ʿ� ����
        }
        else
        {
            // CameraFacing (�⺻)
            _dragPlane = new Plane(_cam.transform.forward * -1f, transform.position);
        }

        if (_dragPlane.Raycast(pointerRay, out float enter))
        {
            var hitPoint = pointerRay.GetPoint(enter);
            _grabOffsetWorld = transform.position - hitPoint;
        }
        OnDragStarted?.Invoke();
    }


    public void DragUpdate(Ray pointerRay)
    {
        if (!_dragging) return;

        Vector3 target;

        if (planeMode == PlaneMode.ScreenDepth)
        {
            // ���� �������� ȭ�� ��ǥ�� ���� ���̷� ���� ��ȯ
            var mouse = UnityEngine.Input.mousePosition;
            var worldAtDepth = _cam.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, _grabDepthZ));
            target = worldAtDepth + _grabOffsetWorld;
        }
        else
        {
            if (!_dragPlane.Raycast(pointerRay, out float enter)) return;
            target = pointerRay.GetPoint(enter) + _grabOffsetWorld;
        }

        // ���
        var cur = transform.position;
        if (lockX) target.x = cur.x;
        if (lockY) target.y = cur.y;
        if (lockZ) target.z = cur.z;

        // ����
        if (snapStep != Vector3.zero)
        {
            if (snapStep.x != 0) target.x = Mathf.Round(target.x / snapStep.x) * snapStep.x;
            if (snapStep.y != 0) target.y = Mathf.Round(target.y / snapStep.y) * snapStep.y;
            if (snapStep.z != 0) target.z = Mathf.Round(target.z / snapStep.z) * snapStep.z;
        }

        // ���
        if (bounds != null) target = bounds.ClosestPoint(target);

        transform.position = target;
    }


    public void EndDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        OnDragEnded?.Invoke();
    }
}
