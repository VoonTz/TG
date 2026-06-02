using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Tilemaps;

public class EnemySpawnerParallel : MonoBehaviour
{
    [Header("Referências")]
    public Tilemap tilemap;
    public Transform player;
    public GameObject enemyPrefab;

    [Header("Configurações de Spawn")]
    public int enemiesPerWave = 20;
    public float spawnHeight = -1f;
    public float minDistanceFromPlayer = 8f;

    [Header("Timer de Spawn")]
    public float timeBetweenWaves = 12f;      // Tempo entre uma wave e outra
    public bool startSpawningAutomatically = true;

    private BoundsInt mapBounds;
    private float spawnTimer = 0f;

    private void Start()
    {
        if (tilemap != null)
            mapBounds = tilemap.cellBounds;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Se quiser começar automaticamente
        if (startSpawningAutomatically)
            spawnTimer = timeBetweenWaves; // Começa a primeira wave mais rápido
    }

    private void Update()
    {
        if (!startSpawningAutomatically) return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnWave();
            spawnTimer = timeBetweenWaves;   // Reinicia o timer
        }
    }

    public void SpawnWave()
    {
        if (tilemap == null || player == null) return;

        Vector2[] positions = new Vector2[enemiesPerWave];

        // === PARALELISMO ===
        Parallel.For(0, enemiesPerWave, i =>
        {
            positions[i] = GenerateValidRandomPosition();
        });
        // ===================

        // Instancia no Main Thread
        for (int i = 0; i < positions.Length; i++)
        {
            Vector3 spawnPos = new Vector3(positions[i].x, positions[i].y, spawnHeight);
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }

        Debug.Log($"Wave spawnada! ({enemiesPerWave} inimigos)");
    }

    private Vector2 GenerateValidRandomPosition()
    {
        int attempts = 0;
        const int maxAttempts = 50;

        while (attempts < maxAttempts)
        {
            float x = Random.Range(mapBounds.xMin, mapBounds.xMax - 1);
            float y = Random.Range(mapBounds.yMin, mapBounds.yMax - 1);

            Vector3Int cellPos = new Vector3Int(Mathf.FloorToInt(x), Mathf.FloorToInt(y), 0);

            if (tilemap.HasTile(cellPos))
            {
                Vector2 worldPos = new Vector2(x + 0.5f, y + 0.5f);

                // Evita spawn perto do jogador
                if (Vector2.Distance(worldPos, player.position) >= minDistanceFromPlayer)
                {
                    return worldPos;
                }
            }

            attempts++;
        }

        // Fallback seguro
        return new Vector2(player.position.x + Random.Range(10f, 15f), player.position.y);
    }
}