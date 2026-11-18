#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// EditModeテスト用に、Sceneを使わずGridManagerとBlockを組み立てます。
/// EditModeではAwakeが呼ばれないため、非公開フィールドと初期化メソッドをリフレクションで設定します。
/// 作成したObjectはDisposeでまとめて破棄します。
/// </summary>
internal sealed class GridTestUtility : System.IDisposable
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly List<Object> _created = new List<Object>();
    private BlockSettings _breakableSettings;
    private BlockSettings _unbreakableSettings;

    public GridManager Grid { get; }

    public GridTestUtility(Vector3Int size)
    {
        GridSettings settings = Track(ScriptableObject.CreateInstance<GridSettings>());
        SetField(settings, "_size", size);
        SetField(settings, "_cellSize", 1f);

        GameObject gridObject = Track(new GameObject("Test Grid"));
        Grid = gridObject.AddComponent<GridManager>();
        SetField(Grid, "_settings", settings);
        typeof(GridManager).GetMethod("InitializeGrid", PrivateInstance).Invoke(Grid, null);
    }

    /// <summary>通常セルへBlockを登録します。</summary>
    public Block AddBlock(Vector3Int position, BlockType type)
    {
        Block block = CreateBlock(type);
        if (!Grid.TryRegisterBlock(position, block))
            throw new System.InvalidOperationException($"Blockを登録できません: {position}");

        return block;
    }

    /// <summary>y=-1の外殻へ床を敷きます（経路探索で立てる足場になります）。</summary>
    public void AddFloor()
    {
        Vector3Int size = Grid.Size;
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.z; z++)
                Grid.TryRegisterBoundaryBlock(new Vector3Int(x, -1, z), CreateBlock(BlockType.Unbreakable));
        }
    }

    public void Dispose()
    {
        for (int i = _created.Count - 1; i >= 0; i--)
        {
            if (_created[i] != null)
                Object.DestroyImmediate(_created[i]);
        }

        _created.Clear();
    }

    private Block CreateBlock(BlockType type)
    {
        Block block = Track(new GameObject($"Test Block ({type})")).AddComponent<Block>();
        SetField(block, "_settings", GetBlockSettings(type));
        return block;
    }

    private BlockSettings GetBlockSettings(BlockType type)
    {
        ref BlockSettings cached = ref (type == BlockType.Breakable ? ref _breakableSettings : ref _unbreakableSettings);
        if (cached == null)
        {
            cached = Track(ScriptableObject.CreateInstance<BlockSettings>());
            SetField(cached, "_type", type);
        }

        return cached;
    }

    private T Track<T>(T target) where T : Object
    {
        _created.Add(target);
        return target;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, PrivateInstance);
        if (field == null)
            throw new TestSetupException(target.GetType().Name, name);

        field.SetValue(target, value);
    }

    private sealed class TestSetupException : System.Exception
    {
        public TestSetupException(string type, string field)
            : base($"{type}.{field} が見つかりません。フィールド名を変更した場合はテスト側も更新してください。") { }
    }
}
#endif
