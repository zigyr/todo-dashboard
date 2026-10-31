import assert from "node:assert/strict";
import { normalizeUrl } from "./validate.ts";

function check(name: string, fn: () => void) {
  try {
    fn();
    console.log(`✓ ${name}`);
  } catch (error) {
    console.error(`✗ ${name}`);
    throw error;
  }
}

check("给裸域名补上 https", () => {
  assert.equal(normalizeUrl("example.com/post"), "https://example.com/post");
});

check("保留已有的 http 和 https", () => {
  assert.equal(normalizeUrl("http://example.com/a"), "http://example.com/a");
  assert.equal(normalizeUrl("https://example.com/a?b=1"), "https://example.com/a?b=1");
});

check("去掉首尾空格", () => {
  assert.equal(normalizeUrl("  https://example.com/a  "), "https://example.com/a");
});

check("拒绝非 http(s) 协议", () => {
  assert.equal(normalizeUrl("ftp://example.com/a"), null);
  assert.equal(normalizeUrl("javascript:alert(1)"), null);
  assert.equal(normalizeUrl("file:///C:/secret.txt"), null);
});

check("拒绝空输入", () => {
  assert.equal(normalizeUrl(""), null);
  assert.equal(normalizeUrl("   "), null);
});

check("拒绝明显不合法的地址", () => {
  assert.equal(normalizeUrl("not a url"), null);
  assert.equal(normalizeUrl("https://"), null);
});

console.log("all checks passed");
