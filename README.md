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
| Enter | Começar (tela inicial) / jogar a fase escolhida (mapa de fases) |

As teclas de movimento e de reiniciar podem ser trocadas em **Esc → Controles** (clique no comando e depois na tecla; há botão para restaurar o padrão). A configuração fica salva.

- Pegue as chaves **amarela, vermelha e azul** e abra as portas da mesma cor.
- A porta final precisa das 3 chaves.
- Algumas fases são escuras. Ache a **lanterna** para iluminar tudo.
- Há fases com 2 ou 3 andares, ligados por **escadas**.
- Ao chegar na saída aparece o menu de fim de fase: próxima fase, reiniciar, mapa de fases ou tela inicial.
- No mapa de fases, WASD/setas movem para a direção em que está a bolinha da fase. O melhor tempo de cada fase fica salvo.

## Como rodar o projeto

1. Feche o VS Code.
2. Abra o Godot pelo `iniciar.bat` (setup portátil, não precisa instalar nada).
3. Abra o projeto e aperte **F5** (cena principal: `Title.tscn`).
4. Se mexer em arquivos `.cs`, clique no martelo (Build) antes de rodar.

## Tela inicial

Cenário em pixel art (384×216, ampliado 3×) que muda sozinho entre **dia, entardecer, noite e amanhecer**:

- Céu em degradê, nuvens que andam, estrelas, vaga-lumes e uma nave cruzando o céu.
- Castelo numa colina, floresta, lago com ponte, cidade, templo grego, catedral gótica, deserto com pirâmides e um vulcão (cada marco antecipa um futuro mundo).
- Janelas acesas à noite e lava do vulcão brilhando.
- **Clique no sol ou na lua** para avançar a hora.
- Está em camadas (`far`, `lava`, `mid`, `front`, `lights`), cada uma tingida de um jeito conforme a hora (`Title.cs`).

## Fases

As fases são arquivos de texto em `maps/faseN.txt`. Basta criar `fase13.txt` que o jogo conta sozinho.

Uma fase pode ter vários andares: separe cada andar com uma linha contendo só `---`. A escada `>` de um andar liga com a `<` do andar de cima (uma de cada por andar).

| Letra | Significa |
|---|---|
| `#` | Parede |
| `P` | Início do jogador |
| `y` `r` `b` | Chave amarela / vermelha / azul |
| `Y` `R` `B` | Porta amarela / vermelha / azul |
| `D` | Porta final (precisa das 3 chaves) |
| `S` | Saída (portal) |
| `>` | Escada para o próximo andar |
| `<` | Escada para o andar de baixo |
| `L` | Lanterna |
| espaço | Chão |

Cada fase tem exatamente uma chave de cada cor. As fases 1 a 3 foram feitas à mão; as 4 a 12 foram geradas por um script e verificadas por um solucionador (todas têm solução e todas as chaves são necessárias).

## Estrutura de pastas

```
Lockbound/
├── art/                 Sprites em pixel art (PNG)
│   ├── player/          Personagem: player_* (parado) e walk_* (6 quadros) em 4 direções
│   ├── environment/     Parede, chão (3 variações), saída, escadas
│   ├── items/           Chave, portas (amarela/vermelha/azul/final), lanterna
│   ├── title/           Cenário da tela inicial (camadas, sol, lua, nuvens, nave)
│   └── ui/              (vazia por enquanto) ícones do HUD
├── maps/                Fases em texto (fase1.txt ... fase12.txt)
├── Maze.cs              Jogo (mapa, andares, chaves, portas, HUD, escuridão)
├── Player.cs            Movimento e animação do personagem
├── Title.cs             Tela inicial dinâmica
├── LevelSelect.cs       Mapa de fases
├── PauseMenu.cs         Menu de pausa e tela de Controles
├── FinishMenu.cs        Menu de fim de fase
├── GameControls.cs      Teclas configuráveis (salvas em user://controls.cfg)
├── Progess.cs           Progresso e melhores tempos (user://progress.cfg)
├── *.tscn               Cenas (Title, LevelSelect, Main, Player)
└── project.godot
```

## Padrão da arte

- Bloco do mapa: **48×48 px**, tudo com escala 1.
- Personagem: imagem parada 32×32 e quadros de andar 48×48, desenhado com escala 1,5 no código. O colisor fica nos pés e é um pouco mais baixo em cima (efeito 3D).
- A animação de andar segue a distância percorrida (`StridePixels` no `Player.cs`), não o relógio.
- Filtro de textura **Nearest** (senão o pixel art fica borrado).
- Chaves e lanterna são sprites próprios; as portas já vêm coloridas (marrom com detalhes na cor da chave).
- Tela inicial: arte em 384×216, 1 pixel da arte = 3×3 px na tela.

## Planos para os mundos (ainda não feito)

Cada mundo terá um tema, um conjunto de fases e liberará uma skin:

1. Castelo / masmorra (mundo atual)
2. Deserto e pirâmides
3. Vulcão
4. Ruínas greco-romanas / minotauro
5. Atlântida (rio)
6. Trem gótico (inclui o jardim vitoriano, catedral)
7. Cidade steampunk
8. Nave espacial / cyberespaço
9. (a definir)

Mais 3 mundos secretos (ideias: Doces, Céu, Espelhos). Mecânicas possíveis por mundo: areia que atrasa, gelo que escorrega, lava, esteiras, teleportes, alavancas, controles invertidos.

## Próximos passos

- [x] Sprites de parede, chão, itens e personagem (4 direções)
- [x] Animação de andar
- [x] 12 fases (até 3 andares, com escadas)
- [x] Menu de pausa, controles configuráveis, menu de fim de fase
- [x] Tela inicial dinâmica (dia/noite)
- [ ] Ações completas com texto no meio da tela
- [ ] Funcionalidades que 
- [ ] Panorama da tela inicial refeito com arte melhor (em andamento)
- [ ] Sistema de mundos (pastas `maps/mundoN`, temas e skins)
- [ ] Música e efeitos sonoros
- [ ] Moedas/XP, bônus de tempo, bússola
- [ ] Skins de personagem
- [ ] Estrelas por tempo no mapa de fases
- [ ] Ícones do HUD (`art/ui`)
- [ ] Versão 3D em primeira pessoa
