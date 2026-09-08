using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 1つのGridManagerにつき1つのプール。Prefab別に待機個体を保存します。
/// 不足時だけ生成し、返却後は非表示にして次回利用。Sceneを跨いで保持しません。
/// </summary>
[DisallowMultipleComponent]
public class GridObjectPool : MonoBehaviour
{
    private readonly Dictionary<PooledGridObject, Stack<PooledGridObject>> _available = new();
    private readonly Dictionary<PooledGridObject, PooledGridObject> _sources = new();
    private readonly HashSet<PooledGridObject> _rented = new();
    private Transform _inactiveRoot;

    /// <summary>追加のInspector設定なしで、同じグリッドの生成元がプールを共有できます。</summary>
    public static GridObjectPool For(GridManager grid)
    {
        GridObjectPool pool = grid.GetComponent<GridObjectPool>();
        return pool != null ? pool : grid.gameObject.AddComponent<GridObjectPool>();
    }

    public T Rent<T>(T prefab, Vector3 position, Quaternion rotation) where T : PooledGridObject
    {
        if (!_available.TryGetValue(prefab, out Stack<PooledGridObject> stack))
        {
            stack = new Stack<PooledGridObject>();
            _available.Add(prefab, stack);
        }

        PooledGridObject instance = null;
        // 外部からDestroyされた待機個体があっても飛ばします。
        while (stack.Count > 0 && instance == null)
        {
            instance = stack.Pop();
            if (instance == null && !ReferenceEquals(instance, null))
                _sources.Remove(instance);
        }

        if (instance == null)
        {
            if (_inactiveRoot == null)
            {
                GameObject root = new GameObject("Pooled Objects (Inactive)");
                root.transform.SetParent(transform, false);
                root.SetActive(false);
                _inactiveRoot = root.transform;
            }
            // 非アクティブな親の下で生成し、初期化前のOnEnableを防ぎます。
            instance = Instantiate(prefab, _inactiveRoot);
            instance.gameObject.SetActive(false);
            _sources.Add(instance, prefab);
        }

        instance.transform.SetParent(transform, false);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = prefab.transform.localScale;
        instance.PrepareForRent(this);
        _rented.Add(instance);
        instance.gameObject.SetActive(true);
        return (T)instance;
    }

    internal void Return(PooledGridObject instance)
    {
        // 取得と爆風が重なっても、同じ個体を二度Stackへ戻しません。
        if (!_rented.Remove(instance))
            return;

        instance.ClearForPool();
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(_inactiveRoot, false);
        _available[_sources[instance]].Push(instance);
    }
}
