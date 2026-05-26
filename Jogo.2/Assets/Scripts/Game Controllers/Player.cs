using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

// Classe responsável por controlar o jogador
public class Player : MonoBehaviour
{
    // =========================
    // COMPONENTES BÁSICOS
    // =========================

    // Rigidbody 2D do jogador (movimentação física)
    private Rigidbody2D rigidbody2D;

    // Vetor de movimento atual
    private Vector2 movement;

    // Controle da direção que o personagem está olhando
    private bool facingRight = true;

    // Referência à câmera (usada para efeito de tremor)
    public GameObject camera;

    // =========================
    // VIDA E PODERES
    // =========================

    // Lista de imagens de coração (vidas do jogador)
    public List<GameObject> Hearts = new List<GameObject>(3);

    // Referência ao escudo que segue o jogador
    [SerializeField]
    public FollowShield shield;

    // Tela de Game Over
    [SerializeField]
    public GameObject gameOverScreen;

    // Tela exibida quando a conexão falha
    public GameObject fallConnectionScreen;

    // =========================
    // VARIÁVEIS DE JOGO
    // =========================

    // Velocidade do jogador
    public float speed;

    // Quantidade atual de vidas
    private int life;

    // Contadores de itens/colisões
    private int countShell = 0;
    private int countObstacle = 0;
    private int countLife = 0;
    private int countShield = 0;

    // Define se o controle será manual (teclado)
    private bool manualMode;

    // Guarda o estado anterior da conexão da balança
    private bool lastConnectionState = false;

    // =========================
    // UI
    // =========================

    // Texto que mostra a quantidade de conchas coletadas
    [SerializeField]
    public TMP_Text countShellText;

    // =========================
    // WII BALANCE BOARD
    // =========================

    [Header("Configuração")]
    // Índice do Wii Remote conectado à Balance Board
    public static int remoteIndex = 0;

    // =========================
    // SD BALANCE (ARDUINO)
    // =========================

    // Referência ao script de comunicação serial
    private SD_Serial _sd_serial;

    // Margem de tolerância para movimentação (zona morta)
    public float renge = 10;

    // Peso calibrado do jogador
    public float PesoCalibrado = 0;

    // Valores de peso do lado esquerdo e direito
    public float Esquerda = 0;
    public float Direita = 0;

    // =========================
    // MÉTODOS UNITY
    // =========================

    void Start()
    {
        // Obtém o Rigidbody2D do jogador
        rigidbody2D = GetComponent<Rigidbody2D>();

        // Define a vida inicial com base na quantidade de corações
        life = Hearts.Count;

        // Verifica se a Balance Board está conectada
        bool isBoardConnected = Wii.IsActive(remoteIndex) && Wii.GetExpType(remoteIndex) == 3;

        // Se não houver Balance Board, ativa o modo manual (teclado)
        manualMode = !isBoardConnected;
    }

    void Update()
    {
        // 1. Prioridade de controle: se não houver serial conectada, tenta balança Wii, senão Teclado
        if (SD_Serial._connected) 
        {
            SDBalanceMove();
        }
        else if (Wii.IsActive(remoteIndex))
        {
            NintendoBalanceBoardMove();
        }
        else if (manualMode)
        {
            KeyboardMove();
        }
    }

// Corrija o SDBalanceMove para usar a variável 'movement'
    void SDBalanceMove()
    {
        if (_sd_serial == null) return;

        PesoCalibrado = _sd_serial.P;
        Esquerda = (_sd_serial.A + _sd_serial.C);
        Direita = (_sd_serial.B + _sd_serial.D);

        float threshold = (PesoCalibrado / 2) + renge;

        if (Esquerda > threshold)
        {
            movement = new Vector2(-1, 0);
            if (facingRight) Flip();
        }
        else if (Direita > threshold)
        {
            movement = new Vector2(1, 0);
            if (!facingRight) Flip();
        }
        else
        {
            movement = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        // Aplica a velocidade ao Rigidbody
        rigidbody2D.linearVelocity = movement * speed;
    }

    // =========================
    // COLISÕES
    // =========================

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Caso colida com um obstáculo
        if (collision.gameObject.tag == "Obstacle")
        {
            // Ativa o tremor da câmera
            camera.GetComponent<Tremor>().playTremor();

            // Reduz vida
            life--;
            countObstacle++;

            // Desativa o coração correspondente
            Hearts[life].SetActive(false);

            // Retorna o obstáculo ao pool
            ObjectPool.Instance.ReturnToPool("Obstacle", collision.gameObject);

            // Caso o jogador perca todas as vidas
            if (life == 0)
            {
                Destroy(gameObject);
                Time.timeScale = 0f;
                gameOverScreen.SetActive(true);
            }
        }

        // Caso colida com item de vida
        if (collision.gameObject.CompareTag("Life"))
        {
            // Se já estiver com vida máxima
            if (life == 3)
            {
                ObjectPool.Instance.ReturnToPool("Life", collision.gameObject);
            }

            // Recupera vida se for menor que o máximo
            if (life < 3)
            {
                life++;
                countLife++;
                Hearts[life - 1].SetActive(true);
                ObjectPool.Instance.ReturnToPool("Life", collision.gameObject);
            }
        }

        // Caso colida com uma concha
        if (collision.gameObject.CompareTag("Shell"))
        {
            countShell++;
            countShellText.text = countShell.ToString();
            ObjectPool.Instance.ReturnToPool("Shell", collision.gameObject);
        }

        // Caso colida com escudo
        if (collision.gameObject.CompareTag("Shield"))
        {
            countShield++;
            shield.gameObject.SetActive(true);
            ObjectPool.Instance.ReturnToPool("Shield", collision.gameObject);
        }
    }

    // =========================
    // CONTROLE DE DIREÇÃO
    // =========================

    void Flip()
    {
        // Alterna o lado que o personagem está olhando
        facingRight = !facingRight;

        // Inverte a escala no eixo X
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // =========================
    // CONTROLE POR TECLADO
    // =========================

    void KeyboardMove()
    {
        if (Input.GetKey(KeyCode.A))
        {
            movement = new Vector2(-1, 0).normalized;

            if (facingRight)
            {
                Flip();
            }
        }
        else if (Input.GetKey(KeyCode.D))
        {
            movement = new Vector2(1, 0).normalized;

            if (!facingRight)
            {
                Flip();
            }
        }
        else
        {
            movement = Vector2.zero;
        }
    }

    // =========================
    // CONTROLE POR SD BALANCE
    // =========================

    

    // =========================
    // CONTROLE POR WII BALANCE BOARD
    // =========================

    void NintendoBalanceBoardMove()
    {
        // Se o Wii Remote não estiver ativo, sai
        if (!Wii.IsActive(remoteIndex))
        {
            return;
        }

        // Verifica se o acessório conectado é a Balance Board
        if (Wii.GetExpType(remoteIndex) == 3)
        {
            // Lê os sensores da Balance Board
            Vector4 sensors = Wii.GetBalanceBoard(remoteIndex);

            // Filtragem de ruído dos sensores
            if (sensors.x > 0f && sensors.x < 1.3f) sensors.x = 0f;
            else if (sensors.y > -1f && sensors.y < 0f) sensors.y = 0f;
            else if (sensors.w > -1f && sensors.w < 0f) sensors.w = 0f;
            else if (sensors.z > 0 && sensors.z < 2.90f) sensors.z = 0f;

            // Movimento para a esquerda
            if ((sensors.y + sensors.w) > (BalanceBoardCalibration.playerWeight / 2) + 5)
            {
                movement = new Vector2(-1, 0);
                if (facingRight) Flip();
            }
            // Movimento para a direita
            else if (sensors.x + sensors.z > (BalanceBoardCalibration.playerWeight / 2) + 5)
            {
                movement = new Vector2(1, 0);
                if (!facingRight) Flip();
            }
            else
            {
                movement = Vector2.zero;
            }

            // Exibe os valores dos sensores no console
            Debug.Log(
                $"Quadrante 1: {sensors.x:F2} kg; " +
                $"Quadrante 2: {sensors.y:F2} kg; " +
                $"Quadrante 3: {sensors.w:F2} kg; " +
                $"Quadrante 4: {sensors.z:F2} kg"
            );
        }
    }
}