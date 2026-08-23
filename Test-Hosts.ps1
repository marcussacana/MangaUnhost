# Testa a acessibilidade de cada host suportado pelo MangaUnhost (MangaUnhost/Hosts/*.cs)
# e abre no navegador padrao qualquer um que parecer fora do ar.
#
# Uso: pwsh -File Test-Hosts.ps1 [-Open]
#   -Open  abre automaticamente os hosts marcados como DEAD no navegador padrao.
#
# OBS: hosts marcados como GenericPlugin (WordpressManga, WPMangaReader, SussyToons,
# MangaNelo) tambem casam por HTML de qualquer dominio nao mapeado (IsValidPage), entao
# o dominio de exemplo abaixo pode "morrer" sem que o plugin em si fique inutil. Nao
# remova esses arquivos so por causa do resultado deste teste.

param(
    [switch]$Open
)

$ProgressPreference = 'SilentlyContinue'

$HostsToTest = @(
    @{ Name = "BaoZimh";        Url = "https://www.baozimh.com/" }
    @{ Name = "BlackoutComics"; Url = "https://blackoutcomics.com/" }
    @{ Name = "ComixTo";        Url = "https://comix.to/title/0q23v-i-dont-want-to-play-matchmaker" }
    @{ Name = "DragonScanNext"; Url = "https://rfdragonscan.net/ecdc6b97-c3b8-4318-a34c-a8282f0a29e6/o-vilao-de-cabelos-amarelos-no-romance-da-personagem-principal-feminina-quer-felicidade" }
    @{ Name = "HentaiNexus";    Url = "https://hentainexus.com/" }
    @{ Name = "Lycantoons";     Url = "https://lycantoons.com/series/a-esposa-que-esperou-no-campo-de-trigo" }
    @{ Name = "MangaDex";       Url = "https://mangadex.org/title/a9c157f6-a720-4adc-87a4-79a3601aa4e8/10-nenmae-ni-time-leap-shite-osananajimi-no-ojousama-wo-tasuketara-iinazuke-ni-narimashita" }
    @{ Name = "MangaFire";      Url = "https://mangafire.to/manga/chiisakute-kawaii-bungeibu-no-chiteki-na-senpai-o-hiza-no-ue-ni-nosetara-mainichi-suwattekuru-you-ni-natta.x15x8" }
    @{ Name = "MangaHere";      Url = "https://www.mangahere.cc/manga/hima_ten/" }
    @{ Name = "MangaNelo";      Url = "https://mangakakalot.gg/" }
    @{ Name = "Mangago";        Url = "https://www.mangago.me/read-manga/today_is_a_woman_day/" }
    @{ Name = "Mediocretoons";  Url = "https://mediocretoons.com/obra/124" }
    @{ Name = "NHentai";        Url = "https://nhentai.net/" }
    @{ Name = "NexusToons";     Url = "https://nexustoons.com/manga/invocador-de-demonios-do-abismo" }
    @{ Name = "SussyToons";     Url = "https://empreguetes.xyz/obra/10714/uma-princesa-que-le-a-sorte" }
    @{ Name = "SussyToons(wtf)";Url = "https://www.sussytoons.wtf/obra/eu-realmente-nao-sou-o-lacaio-do-deus-demonio" }
    @{ Name = "Taiyo";          Url = "https://taiyo.moe/" }
    @{ Name = "WPMangaReader";  Url = "https://mangaschan.net/" } # generico: dominio direto morto, plugin ainda vive via IsValidPage
    @{ Name = "Webtoons";       Url = "https://www.webtoons.com/" }
    @{ Name = "Weloma";         Url = "https://weloma.art/" }
    @{ Name = "WordpressManga"; Url = "https://mangalivre.blog/manga/mob-kara-hajimaru-tansaku-eiyuutan/" }
    @{ Name = "Yomu";           Url = "https://yomu.com.br/obra/a-academia-esta-condenada" }
)

$dead = @()

foreach ($h in $HostsToTest) {
    Write-Host -NoNewline ("{0,-18} " -f $h.Name)
    try {
        $r = Invoke-WebRequest -Uri $h.Url -UserAgent "Mozilla/5.0" -TimeoutSec 10 -MaximumRedirection 5 -ErrorAction Stop
        Write-Host "OK ($($r.StatusCode))" -ForegroundColor Green
    } catch {
        $resp = $_.Exception.Response
        if ($resp -and $resp.StatusCode) {
            Write-Host "UP, HTTP $([int]$resp.StatusCode)" -ForegroundColor Yellow
        } else {
            Write-Host "DEAD: $($_.Exception.Message)" -ForegroundColor Red
            $dead += $h
        }
    }
}

if ($dead.Count -eq 0) {
    Write-Host "`nTodos os hosts responderam." -ForegroundColor Green
    return
}

Write-Host "`nHosts fora do ar:" -ForegroundColor Red
$dead | ForEach-Object { Write-Host " - $($_.Name): $($_.Url)" }

if ($Open) {
    foreach ($h in $dead) { Start-Process $h.Url }
}
