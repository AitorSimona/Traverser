using Unity.Collections;
using Unity.SnapshotDebugger;
using Unity.Kinematica;

using UnityEngine;

using Identifier = Unity.SnapshotDebugger.Identifier<Unity.SnapshotDebugger.Aggregate>;

public class Spawner : SnapshotProvider
{
    public GameObject prefab;

    public float timeToSpawn;

    public float timeToLive;

    [Snapshot]
    float timeInSeconds;

    [Snapshot]
    float accumulatedTime;

    NativeList<SpawnedObject> spawnedObjects;

    public struct SpawnedObject
    {
        Identifier identifier;

        float timeInSeconds;

        public static SpawnedObject Create(GameObject gameObject)
        {
            return new SpawnedObject
            {
                identifier = Debugger.instance[gameObject]
            };
        }

        public bool Update(float deltaTime, float timeToLive)
        {
            timeInSeconds += deltaTime;

            if(timeInSeconds >= timeToLive)
            {
                Destroy(Debugger.instance[identifier]);

                return true;
            }

            return false;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();

        spawnedObjects = new NativeList<SpawnedObject>(16, Allocator.Persistent);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        spawnedObjects.Dispose();
    }

    public override void WriteToStream(Buffer buffer)
    {
        base.WriteToStream(buffer);

        if(spawnedObjects.IsCreated)
        {
            spawnedObjects.WriteToStream(buffer);
        }
    }

    public override void ReadFromStream(Buffer buffer)
    {
        base.ReadFromStream(buffer);

        spawnedObjects.ReadFromStream(buffer);
    }

    void Update()
    {
        var deltaTime = Debugger.instance.deltaTime;

        UpdateTransform(deltaTime);

        accumulatedTime += deltaTime;

        if (accumulatedTime >= timeToSpawn)
        {
            accumulatedTime -= timeToSpawn;

            spawnedObjects.Add(
                SpawnedObject.Create(
                    Instantiate(prefab,
                        transform.position, transform.rotation)));
        }

        for (int i = spawnedObjects.Length - 1; i >= 0; i--)
        {
            if (spawnedObjects.At(i).Update(deltaTime, timeToLive))
            {
                spawnedObjects.RemoveAtSwapBack(i);
            }
        }
    }

    void UpdateTransform(float deltaTime)
    {
        Vector3 GetPositionAtTime(float t)
        {
            float modTwoPi = t % (2.0f * Mathf.PI);

            return new Vector3(
                Mathf.Sin(modTwoPi), 0.0f,
                    Mathf.Cos(modTwoPi));
        }

        float t0 = timeInSeconds;
        float t1 = timeInSeconds + deltaTime;

        var displacement =
            GetPositionAtTime(t1) -
                GetPositionAtTime(t0);

        transform.position += displacement;

        timeInSeconds += deltaTime;
    }
}
