import assert from 'node:assert/strict';
import { test } from 'node:test';
import { previewBonus } from './bonus-preview.ts';
test('official example', () => assert.deepEqual(previewBonus(2, '10', 5000, 8), { gross: 500, tax: 40, net: 460 }));
test('fixed midpoint rounds up', () => assert.deepEqual(previewBonus(1, '0.125', 5000, 0), { gross: 0.13, tax: 0, net: 0.13 }));
test('percentage midpoint rounds up', () => assert.equal(previewBonus(2, '12.5', 1, 0)?.gross, 0.13));
test('tax midpoint and net match backend', () => assert.deepEqual(previewBonus(1, '1', 5000, 12.5), { gross: 1, tax: 0.13, net: 0.87 }));
test('invalid values do not show a payable preview', () => {
  for (const value of ['0', '-1', '100.01', 'abc', '1.00001', '']) assert.equal(previewBonus(2, value, 5000, 8), null);
});
test('fixed amount cannot exceed salary', () => assert.equal(previewBonus(1, '5000.01', 5000, 8), null));
