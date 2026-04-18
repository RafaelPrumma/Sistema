import { chromium } from 'playwright';
import fs from 'node:fs/promises';

const baseUrl = 'http://127.0.0.1:5080';
const outDir = 'artifacts/screenshots';

async function ensureNoLayoutBreak(page, name) {
  const issues = await page.evaluate(() => {
    const offenders = [];
    const nodes = [document.documentElement, document.body, ...document.querySelectorAll('header, footer, nav, .app-shell, .content, .container, .container-fluid, .card')];
    for (const el of nodes) {
      if (!el) continue;
      if (el.scrollWidth - el.clientWidth > 6) {
        offenders.push({
          tag: el.tagName,
          className: el.className,
          scrollWidth: el.scrollWidth,
          clientWidth: el.clientWidth
        });
      }
    }
    return offenders;
  });

  if (issues.length) {
    throw new Error(`Layout com overflow horizontal em ${name}: ${JSON.stringify(issues.slice(0, 5))}`);
  }
}

async function screenshot(page, file) {
  await page.screenshot({ path: `${outDir}/${file}`, fullPage: true });
}

async function openThemePanel(page) {
  await page.click('#temaToggle');
  await page.waitForSelector('#temaSidebar.show', { state: 'visible' });
}

async function closeThemePanel(page) {
  if (await page.locator('#temaSidebar.show').count()) {
    await page.click('#temaSidebarClose');
    await page.waitForSelector('#temaSidebar.show', { state: 'hidden' });
  }
}

async function setThemeToggle(page, id, checked) {
  const locator = page.locator(id);
  const isChecked = await locator.isChecked();
  if (isChecked !== checked) {
    await locator.click();
  }
  await page.waitForTimeout(250);
}

async function openSubmenu(page, title) {
  const item = page.locator(`#sidebarMenu .nav-item:has(.nav-link[title="${title}"])`).first();
  await item.locator('.nav-link').click();
  await page.waitForTimeout(250);
}

async function clickSubitem(page, title, subTitle) {
  await openSubmenu(page, title);
  await page.evaluate(({ title, subTitle }) => {
    const selector = `#sidebarMenu .nav-item .menu-subpanel a[title="${subTitle}"]`;
    const candidates = Array.from(document.querySelectorAll(selector));
    const target = candidates.find((el) => {
      const parent = el.closest('.nav-item');
      return parent?.querySelector(`.nav-link[title="${title}"]`);
    });
    if (!target) {
      throw new Error(`Subitem não encontrado: ${title} > ${subTitle}`);
    }

    (target).click();
  }, { title, subTitle });
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(300);
}

const browser = await chromium.launch({ headless: true });
const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
const page = await context.newPage();

try {
  await page.goto(`${baseUrl}/Account/Login`, { waitUntil: 'networkidle' });
  await screenshot(page, '01-login.png');

  await page.fill('input[name="Cpf"]', '00000000000');
  await page.fill('input[name="Senha"]', 'admin123');
  await page.click('button[type="submit"]');
  await page.waitForURL(/\/Home\/Index/);
  await page.waitForLoadState('networkidle');

  await ensureNoLayoutBreak(page, 'home-default');
  await screenshot(page, '02-home-default.png');

  await openThemePanel(page);
  await setThemeToggle(page, '#HeaderFixo', true);
  await closeThemePanel(page);
  await ensureNoLayoutBreak(page, 'header-fixo-on');
  await screenshot(page, '03-home-header-fixo-on.png');

  await openThemePanel(page);
  await setThemeToggle(page, '#HeaderFixo', false);
  await closeThemePanel(page);
  await ensureNoLayoutBreak(page, 'header-fixo-off');
  await screenshot(page, '04-home-header-fixo-off.png');

  await openThemePanel(page);
  await setThemeToggle(page, '#FooterFixo', true);
  await closeThemePanel(page);
  await ensureNoLayoutBreak(page, 'footer-fixo-on');
  await screenshot(page, '05-home-footer-fixo-on.png');

  await openThemePanel(page);
  await setThemeToggle(page, '#MenuLateralExpandido', false);
  await closeThemePanel(page);
  await ensureNoLayoutBreak(page, 'menu-colapsado');
  await screenshot(page, '06-home-menu-colapsado.png');

  await openThemePanel(page);
  await setThemeToggle(page, '#MenuLateralExpandido', true);
  await setThemeToggle(page, '#FooterFixo', false);
  await closeThemePanel(page);
  await ensureNoLayoutBreak(page, 'menu-expandido');
  await screenshot(page, '07-home-menu-expandido.png');

  const routes = [
    ['Home', 'Privacidade', '08-privacy.png'],
    ['Acesso', 'Segurança', '09-account-change-password.png'],
    ['Comunicação', 'Feed', '10-mensagem-feed.png'],
    ['Comunicação', 'Caixa de saída', '11-mensagem-caixa-saida.png'],
    ['Comunicação', 'Nova publicação', '12-mensagem-nova.png'],
    ['Administração', 'Configurações', '13-configuracao-index.png'],
    ['Administração', 'Tema', '14-tema-edit.png'],
    ['Documentação', 'Mensagens', '15-documentacao-mensagens.png'],
    ['Documentação', 'Logs e auditoria', '16-documentacao-logs.png'],
    ['Documentação', 'Scripts', '17-documentacao-scripts.png']
  ];

  for (const [title, subTitle, file] of routes) {
    await clickSubitem(page, title, subTitle);
    await ensureNoLayoutBreak(page, file);
    await screenshot(page, file);
  }

  const mobile = await context.newPage();
  await mobile.setViewportSize({ width: 390, height: 844 });
  await mobile.goto(`${baseUrl}/Home/Index`, { waitUntil: 'networkidle' });
  await mobile.click('.mobile-menu-toggle');
  await mobile.waitForSelector('body.menu-mobile-open');
  await ensureNoLayoutBreak(mobile, 'mobile-menu-open');
  await screenshot(mobile, '18-mobile-menu-open.png');

  await openThemePanel(mobile);
  await setThemeToggle(mobile, '#HeaderFixo', true);
  await setThemeToggle(mobile, '#FooterFixo', true);
  await closeThemePanel(mobile);
  await ensureNoLayoutBreak(mobile, 'mobile-theme-panel');
  await screenshot(mobile, '19-mobile-theme-panel.png');

  await mobile.close();

  const files = (await fs.readdir(outDir)).filter(f => f.endsWith('.png')).sort();
  console.log('Screenshots geradas:');
  for (const f of files) console.log(` - ${outDir}/${f}`);
} finally {
  await browser.close();
}
