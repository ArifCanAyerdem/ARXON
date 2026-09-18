using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Arixon.Gameplay;

namespace Arixon.Editor
{
    public static class SetupGameArena
    {
        [MenuItem("ARİXON/Kurulum/Adım 08: Oyun Arenası (Rocket League Stadyumu)")]
        public static void BuildArena()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("Play modundayken sahne düzenlenemez. Lütfen oyunu durdurun.");
                return;
            }

            Scene gameScene;
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning("ARİXON: SampleScene bulunamadı, yeni sahne oluşturuluyor...");
                gameScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(gameScene, scenePath);
                
                var originalScenes = EditorBuildSettings.scenes;
                var newScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(originalScenes);
                newScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = newScenes.ToArray();
            }
            else
            {
                gameScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Eski arenayı temizle
            GameObject oldArena = GameObject.Find("ArenaEnvironment");
            if (oldArena != null) GameObject.DestroyImmediate(oldArena);
            
            // Eski zemin kalıntıları varsa temizle (önceki script'ten kalanlar)
            GameObject oldGround = GameObject.Find("ArenaGround");
            if (oldGround != null) GameObject.DestroyImmediate(oldGround);
            GameObject oldWall1 = GameObject.Find("Wall_North");
            if (oldWall1 != null) GameObject.DestroyImmediate(oldWall1);
            GameObject oldWall2 = GameObject.Find("Wall_South");
            if (oldWall2 != null) GameObject.DestroyImmediate(oldWall2);
            GameObject oldWall3 = GameObject.Find("Wall_East");
            if (oldWall3 != null) GameObject.DestroyImmediate(oldWall3);
            GameObject oldWall4 = GameObject.Find("Wall_West");
            if (oldWall4 != null) GameObject.DestroyImmediate(oldWall4);
            GameObject oldGoal1 = GameObject.Find("Goal_Blue");
            if (oldGoal1 != null) GameObject.DestroyImmediate(oldGoal1);
            GameObject oldGoal2 = GameObject.Find("Goal_Red");
            if (oldGoal2 != null) GameObject.DestroyImmediate(oldGoal2);

            // Ana parent oluştur
            GameObject arenaParent = new GameObject("ArenaEnvironment");

            // 1. Devasa Zemin (80x120 metre)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Stadium_Ground";
            ground.transform.SetParent(arenaParent.transform);
            ground.transform.localScale = new Vector3(8, 1, 12); // Plane 10x10 çarpanlıdır = 80x120
            
            // Çim görünümü için materyal
            Renderer gr = ground.GetComponent<Renderer>();
            Material gm = new Material(Shader.Find("Standard"));
            gm.color = new Color(0.1f, 0.4f, 0.15f); // Koyu yeşil çim
            gr.sharedMaterial = gm;

            // 2. Cam Duvarlar ve Tavan (Yükseklik 30m)
            Material glassMat = CreateGlassMaterial();
            
            // Doğu ve Batı duvarları
            CreateWall("Wall_East", new Vector3(40.5f, 15, 0), new Vector3(1, 30, 120), arenaParent, glassMat);
            CreateWall("Wall_West", new Vector3(-40.5f, 15, 0), new Vector3(1, 30, 120), arenaParent, glassMat);
            
            // Kuzey ve Güney duvarları (Kaleler için ortası boşluklu parçalı yapılar)
            // Kale Genişliği = 24m (-12 to 12). Kalan: 80 - 24 = 56m. Sol: 28m, Sağ: 28m.
            // Sol duvar merkezi: -26. Sağ duvar merkezi: 26.
            
            // Kuzey Duvarı Parçaları (Z=60.5f)
            CreateWall("Wall_North_Left", new Vector3(-26, 15, 60.5f), new Vector3(28, 30, 1), arenaParent, glassMat);
            CreateWall("Wall_North_Right", new Vector3(26, 15, 60.5f), new Vector3(28, 30, 1), arenaParent, glassMat);
            CreateWall("Wall_North_Top", new Vector3(0, 20, 60.5f), new Vector3(24, 20, 1), arenaParent, glassMat); // Kale 10m. Üst duvar 10m'den 30'a. Yükseklik 20. Merkez 20.
            
            // Güney Duvarı Parçaları (Z=-60.5f)
            CreateWall("Wall_South_Left", new Vector3(-26, 15, -60.5f), new Vector3(28, 30, 1), arenaParent, glassMat);
            CreateWall("Wall_South_Right", new Vector3(26, 15, -60.5f), new Vector3(28, 30, 1), arenaParent, glassMat);
            CreateWall("Wall_South_Top", new Vector3(0, 20, -60.5f), new Vector3(24, 20, 1), arenaParent, glassMat);
            
            // Görünmez Tavan
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(arenaParent.transform);
            ceiling.transform.position = new Vector3(0, 30, 0);
            ceiling.transform.rotation = Quaternion.Euler(180, 0, 0); // Aşağı bakması için
            ceiling.transform.localScale = new Vector3(8, 1, 12);
            ceiling.GetComponent<Renderer>().enabled = false; // Görünmez

            // 3. Eğimli Rampa Köşeleri (Topun Havalanması İçin)
            Material rampMat = new Material(Shader.Find("Standard"));
            rampMat.color = new Color(0.15f, 0.45f, 0.2f); // Zeminle uyumlu ama biraz açık yeşil
            
            // Doğu/Batı Rampaları (Uzun)
            CreateSlope("Ramp_East", new Vector3(40, 0, 0), new Vector3(10, 10, 120), new Vector3(0, 0, 45), arenaParent, rampMat);
            CreateSlope("Ramp_West", new Vector3(-40, 0, 0), new Vector3(10, 10, 120), new Vector3(0, 0, 45), arenaParent, rampMat);
            
            // Kuzey/Güney Rampaları (Kale hariç parçalı)
            CreateSlope("Ramp_North_Left", new Vector3(-26, 0, 60), new Vector3(28, 10, 10), new Vector3(45, 0, 0), arenaParent, rampMat);
            CreateSlope("Ramp_North_Right", new Vector3(26, 0, 60), new Vector3(28, 10, 10), new Vector3(45, 0, 0), arenaParent, rampMat);
            CreateSlope("Ramp_South_Left", new Vector3(-26, 0, -60), new Vector3(28, 10, 10), new Vector3(45, 0, 0), arenaParent, rampMat);
            CreateSlope("Ramp_South_Right", new Vector3(26, 0, -60), new Vector3(28, 10, 10), new Vector3(45, 0, 0), arenaParent, rampMat);

            // 3.5. Orta Saha Çizgisi ve Dairesi
            Material lineMat = new Material(Shader.Find("Standard"));
            lineMat.color = Color.white;
            
            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "CenterLine";
            centerLine.transform.SetParent(arenaParent.transform);
            centerLine.transform.position = new Vector3(0, 0.01f, 0); // Zemin hizasından çok az yukarıda
            centerLine.transform.localScale = new Vector3(80, 0.02f, 0.5f);
            centerLine.GetComponent<Renderer>().sharedMaterial = lineMat;
            GameObject.DestroyImmediate(centerLine.GetComponent<Collider>()); // Takılmayı önle

            GameObject centerCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerCircle.name = "CenterCircle";
            centerCircle.transform.SetParent(arenaParent.transform);
            centerCircle.transform.position = new Vector3(0, 0.02f, 0);
            centerCircle.transform.localScale = new Vector3(10, 0.01f, 10);
            centerCircle.GetComponent<Renderer>().sharedMaterial = lineMat;
            GameObject.DestroyImmediate(centerCircle.GetComponent<Collider>()); // Takılmayı önle

            GameObject centerDot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerDot.name = "CenterDot";
            centerDot.transform.SetParent(arenaParent.transform);
            centerDot.transform.position = new Vector3(0, 0.03f, 0);
            centerDot.transform.localScale = new Vector3(1, 0.01f, 1);
            centerDot.GetComponent<Renderer>().sharedMaterial = gm; // Çim renginde (içi boş daire gibi gözüksün)
            GameObject.DestroyImmediate(centerDot.GetComponent<Collider>()); // Takılmayı önle

            // 4. Devasa Kaleler (Rocket League Stili)
            // Z=60'da Kırmızı Kale, Z=-60'da Mavi Kale
            CreateComplexGoal("StadiumGoal_Blue", new Vector3(0, 0, -60.5f), 1, Color.blue, arenaParent, false);
            CreateComplexGoal("StadiumGoal_Red", new Vector3(0, 0, 60.5f), 2, Color.red, arenaParent, true);

            // 4.5. Başlangıç Noktaları (2v2 Spawn Points)
            GameObject spawnPointsParent = GameObject.Find("SpawnPoints");
            if (spawnPointsParent != null) GameObject.DestroyImmediate(spawnPointsParent);
            spawnPointsParent = new GameObject("SpawnPoints");
            spawnPointsParent.transform.SetParent(arenaParent.transform);

            // Mavi Takım (Takım 1) - Z=-60 bölgesinde, topa (Z=0) doğru bakacaklar
            CreateSpawnPoint("Spawn_Blue_Player1", new Vector3(-15, 0.5f, -30), 1, spawnPointsParent);
            CreateSpawnPoint("Spawn_Blue_Player2", new Vector3(15, 0.5f, -30), 1, spawnPointsParent);

            // Kırmızı Takım (Takım 2) - Z=60 bölgesinde, topa (Z=0) doğru bakacaklar
            CreateSpawnPoint("Spawn_Red_Player1", new Vector3(15, 0.5f, 30), 2, spawnPointsParent);
            CreateSpawnPoint("Spawn_Red_Player2", new Vector3(-15, 0.5f, 30), 2, spawnPointsParent);

            // 5. Oyun Topunu (GameBall) Merkeze Yerleştir
            GameObject ballObj = GameObject.Find("GameBall");
            if (ballObj == null)
            {
                string prefabPath = "Assets/Prefabs/GameBall.prefab";
                GameObject ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (ballPrefab != null)
                {
                    ballObj = (GameObject)PrefabUtility.InstantiatePrefab(ballPrefab);
                    ballObj.transform.position = new Vector3(0, 5, 0);
                    Debug.Log("GameBall sahneye eklendi.");
                }
            }
            else
            {
                ballObj.transform.position = new Vector3(0, 5, 0);
            }

            EditorSceneManager.SaveScene(gameScene);
            Debug.Log("ARİXON: Rocket League Stadyumu başarıyla inşa edildi!");
            
            // Ana menüye dön
            string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            if (System.IO.File.Exists(mainMenuPath))
            {
                EditorSceneManager.OpenScene(mainMenuPath, OpenSceneMode.Single);
            }
        }

        private static Material CreateGlassMaterial()
        {
            Material m = new Material(Shader.Find("Standard"));
            m.color = new Color(0.8f, 0.9f, 1f, 0.2f); // Çok saydam
            m.SetFloat("_Mode", 3); // Transparent
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_ALPHABLEND_ON");
            m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            return m;
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale, GameObject parent, Material mat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent.transform);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateSlope(string name, Vector3 pos, Vector3 scale, Vector3 eulerAngles, GameObject parent, Material mat)
        {
            GameObject slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.name = name;
            slope.transform.SetParent(parent.transform);
            slope.transform.position = pos;
            slope.transform.localScale = scale;
            slope.transform.rotation = Quaternion.Euler(eulerAngles);
            slope.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateComplexGoal(string name, Vector3 basePos, int teamId, Color color, GameObject parent, bool isNorth)
        {
            GameObject goalParent = new GameObject(name);
            goalParent.transform.SetParent(parent.transform);
            goalParent.transform.position = basePos;
            
            float sign = isNorth ? 1 : -1;
            
            // Kale dışarı (sahadan uzağa) doğru çıkıntı yapacak
            float depth = 8f;
            float width = 24f;
            float height = 10f;
            
            Material goalMat = new Material(Shader.Find("Standard"));
            goalMat.color = color;
            goalMat.EnableKeyword("_EMISSION");
            goalMat.SetColor("_EmissionColor", color * 2.0f); // Daha belirgin ve parlak renk
            
            Material netMat = CreateGlassMaterial();
            netMat.color = new Color(color.r, color.g, color.b, 0.6f); // Ağı da biraz daha belirgin yap
            netMat.EnableKeyword("_EMISSION");
            netMat.SetColor("_EmissionColor", color * 0.5f);

            // Kale Zemini (Oyuncular aşağı düşmesin diye)
            CreateWall(name + "_Floor", basePos + new Vector3(0, -0.5f, sign * depth / 2), new Vector3(width, 1, depth), goalParent, goalMat);

            // Direkler (Kalınlık 1m)
            CreateWall(name + "_LeftPost", basePos + new Vector3(-width/2, height/2, 0), new Vector3(1, height, 1), goalParent, goalMat);
            CreateWall(name + "_RightPost", basePos + new Vector3(width/2, height/2, 0), new Vector3(1, height, 1), goalParent, goalMat);
            CreateWall(name + "_TopBar", basePos + new Vector3(0, height, 0), new Vector3(width + 1, 1, 1), goalParent, goalMat);
            
            // Ağlar (Nets) - Geri, Sol, Sağ ve Üst
            CreateWall(name + "_NetBack", basePos + new Vector3(0, height/2, sign * depth), new Vector3(width, height, 1), goalParent, netMat);
            CreateWall(name + "_NetLeft", basePos + new Vector3(-width/2, height/2, sign * depth / 2), new Vector3(1, height, depth), goalParent, netMat);
            CreateWall(name + "_NetRight", basePos + new Vector3(width/2, height/2, sign * depth / 2), new Vector3(1, height, depth), goalParent, netMat);
            CreateWall(name + "_NetTop", basePos + new Vector3(0, height, sign * depth / 2), new Vector3(width, 1, depth), goalParent, netMat);
            
            // Trigger Alanı (Topun girdiğini anlamak için)
            GameObject triggerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            triggerObj.name = name + "_Trigger";
            triggerObj.transform.SetParent(goalParent.transform);
            triggerObj.transform.position = basePos + new Vector3(0, height/2, sign * (depth / 2));
            triggerObj.transform.localScale = new Vector3(width - 2, height - 1, depth - 1); // Direklerin içine tam sığması için ufaltıldı
            
            Collider c = triggerObj.GetComponent<Collider>();
            c.isTrigger = true;
            triggerObj.GetComponent<Renderer>().enabled = false; // Görünmez trigger
            
            GoalTrigger trigger = triggerObj.AddComponent<GoalTrigger>();
            var field = typeof(GoalTrigger).GetField("_teamId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(trigger, teamId);
        }

        private static void CreateSpawnPoint(string name, Vector3 pos, int teamId, GameObject parent)
        {
            GameObject sp = new GameObject(name);
            sp.transform.SetParent(parent.transform);
            sp.transform.position = pos;
            
            // Yüzünü tam merkeze (topa) dön
            sp.transform.LookAt(new Vector3(0, pos.y, 0));
            
            SpawnPointData data = sp.AddComponent<SpawnPointData>();
            data.TeamID = teamId;
        }
    }
}
