# 🔒 Lockbound

Jogo de labirinto 2D (visão de cima) feito com **Godot 4.7 .NET + C#**.
Colete as chaves, abra as portas e encontre a saída antes que a luz acabe.
A versão 3D em primeira pessoa fica para depois.

## Como jogar

| Tecla | Ação |
|---|---|
| WASD / setas | Mover |
| R | Reiniciar a fase |
| Esc | Pausar |
| Enter | Começar / voltar ao mapa de fases |

- Pegue as chaves **amarela, vermelha e azul** e abra as portas da mesma cor.
- A porta final precisa das 3 chaves.
- O mapa começa escuro. Ache a **lanterna** para iluminar tudo.

## Como rodar o projeto

1. Feche o VS Code.
2. Abra o Godot pelo `iniciar.bat` (setup portátil, não precisa instalar nada).
3. Abra o projeto e aperte **F5** (cena principal: `Title.tscn`).
4. Se mexer em arquivos `.cs`, clique no martelo (Build) antes de rodar.

## Fases

As fases são arquivos de texto em `maps/faseN.txt`. Basta criar `fase4.txt` que o jogo conta sozinho.

| Letra | Significa |
|---|---|
| `#` | Parede |
| `P` | Início do jogador |
| `y` `r` `b` | Chave amarela / vermelha / azul |
| `Y` `R` `B` | Porta amarela / vermelha / azul |
| `D` | Porta final (precisa das 3 chaves) |
| `S` | Saída |
| `L` | Lanterna |
| espaço | Chão |

## Estrutura de pastas

```
Lockbound/
├── art/                 Sprites em pixel art (PNG)
│   ├── player/          Personagem (4 direções)
│   ├── environment/     Parede, chão, saída
│   ├── items/           Chaves, portas, lanterna
│   └── ui/              Ícones do HUD, fundo do título, mapa de fases
├── maps/                Fases em texto (fase1.txt, fase2.txt...)
├── *.cs                 Scripts (Maze, Player, Title, PauseMenu, LevelSelect, Progess)
├── *.tscn               Cenas (Title, LevelSelect, Main, Player)
└── project.godot
```

## Padrão da arte

- Bloco do mapa: **48×48 px**, tudo com escala 1.
- Personagem: canvas 64×64 com a figura de ~32 px.
- Filtro de textura **Nearest** (senão o pixel art fica borrado).
- Chaves e portas são desenhadas em cinza e coloridas pelo código.

## Próximos passos

- [ ] Trocar os retângulos por sprites (parede, chão, itens)
- [ ] Sprite do personagem com 4 direções
- [ ] Mais fases
- [ ] Música e efeitos sonoros
- [ ] Moedas/XP, bônus de tempo, bússola
- [ ] Skins de personagem
- [ ] Estrelas por tempo no mapa de fases
- [ ] Versão 3D em primeira pessoa
