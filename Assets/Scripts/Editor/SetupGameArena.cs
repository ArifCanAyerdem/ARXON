using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Arixon.Gameplay;

namespace Arixon.Editor
{
    public static class SetupGameArena
    {
        [MenuItem("ARİXON/Kurulum/Adım 08: Oyun Arenası ve Kaleler (Fall Guys + Rocket League)")]
        public static void BuildArena()
        {
            string scenePath = "Assets/Scenes/GameScene.unity";
            
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Play modundayken sahne düzenlenemez. Lütfen oyunu durdurun.");
                return;
            }

            Scene gameScene;

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning("ARİXON: GameScene bulunamadı, yeni sahne oluşturuluyor...");
                gameScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(gameScene, scenePath);
                
                // Build Settings'e de ekleyelim
                var originalScenes = EditorBuildSettings.scenes;
                var newScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(originalScenes);
                newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = newScenes.ToArray();
            }
            else
            {
                gameScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // 1. Zemin (Eğer yoksa)
            GameObject ground = GameObject.Find("ArenaGround");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "ArenaGround";
                ground.transform.localScale = new Vector3(3, 1, 5); // 30x50m arena
            }

            // 2. Duvarlar (Karakterlerin veya topun düşmemesi için)
            CreateWall("Wall_North", new Vector3(0, 5, 25), new Vector3(30, 10, 1));
            CreateWall("Wall_South", new Vector3(0, 5, -25), new Vector3(30, 10, 1));
            CreateWall("Wall_East", new Vector3(15, 5, 0), new Vector3(1, 10, 50));
            CreateWall("Wall_West", new Vector3(-15, 5, 0), new Vector3(1, 10, 50));

            // 3. Kaleler (Goals)
            CreateGoal("Goal_Blue", new Vector3(0, 2f, -24f), 1, Color.blue);
            CreateGoal("Goal_Red", new Vector3(0, 2f, 24f), 2, Color.red);

            // 4. Oyun Topunu (GameBall) Merkeze Yerleştir
            GameObject ballObj = GameObject.Find("GameBall");
            if (ballObj == null)
            {
                string prefabPath = "Assets/Prefabs/GameBall.prefab";
                GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (ballPrefab != null)
                {
                    ballObj = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
                    ballObj.transform.position = new Vector3(0, 3, 0);
                    Debug.Log("GameBall sahneye eklendi.");
                }
                else
                {
                    Debug.LogError("GameBall prefab'i bulunamadı! Önce topu oluşturun.");
                }
            }

            EditorSceneManager.SaveScene(gameScene);
            Debug.Log("ARİXON: Oyun Arenası, Kaleler ve Duvarlar başarıyla oluşturuldu!");
            
            // Kullanıcı oyunu test edebilsin diye ana menüye geri dön
            string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            if (System.IO.File.Exists(mainMenuPath))
            {
                EditorSceneManager.OpenScene(mainMenuPath, OpenSceneMode.Single);
            }
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.Find(name);
            if (wall == null)
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = name;
                
                // Saydam cam materyali yapalım
                Renderer r = wall.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Standard"));
                m.color = new Color(0.8f, 0.9f, 1f, 0.3f);
                m.SetFloat("_Mode", 3); // Transparent
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.DisableKeyword("_ALPHABLEND_ON");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
                r.sharedMaterial = m;
            }
            wall.transform.position = pos;
            wall.transform.localScale = scale;
        }

        private static void CreateGoal(string name, Vector3 pos, int teamId, Color color)
        {
            GameObject goal = GameObject.Find(name);
            if (goal == null)
            {
                goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                goal.name = name;
                
                // Kaleye giren şeyleri algılamak için Trigger yap
                Collider c = goal.GetComponent<Collider>();
                c.isTrigger = true;
                
                // Kaleye script'i ekle
                GoalTrigger trigger = goal.AddComponent<GoalTrigger>();
                // teamId'yi Reflection ile atayalım (Private olduğu için)
                var field = typeof(GoalTrigger).GetField("_teamId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) field.SetValue(trigger, teamId);

                // Kalenin görseli
                Renderer r = goal.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (m.shader == null) m = new Material(Shader.Find("Standard"));
                m.color = new Color(color.r, color.g, color.b, 0.5f); // Yarı saydam
                r.sharedMaterial = m;
            }
            
            goal.transform.position = pos;
            goal.transform.localScale = new Vector3(8, 4, 2); // Geniş bir kale
        }
    }
}
