using UnityEngine;
using Unity.Entities;

public class PrefabEntities : MonoBehaviour, IConvertGameObjectToEntity
{
    public static Entity prefabEntity;

    public GameObject prefab;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        Entity prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, dstManager.World);
        PrefabEntities.prefabEntity = prefabEntity;
        // using (BlobBuilder blobAssetStore = new BlobBuilder())
        // {
        // }
    }
}