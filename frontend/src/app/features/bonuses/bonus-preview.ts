/** Preview only. The API remains authoritative. Integer arithmetic avoids binary midpoint errors. */
export function previewBonus(type: number, value: string, salary: number, taxRate: number) {
  if (!/^\d+(\.\d{1,4})?$/.test(value)) return null;
  const scaled = (text: string, digits: number) => {
    const [whole, fraction = ''] = text.split('.');
    return BigInt(whole) * 10n ** BigInt(digits) + BigInt(fraction.padEnd(digits, '0'));
  };
  const roundedDivide = (numerator: bigint, denominator: bigint) => (numerator + denominator / 2n) / denominator;
  const amount = scaled(value, 4);
  const salaryCents = scaled(salary.toFixed(2), 2);
  const taxHundredths = scaled(taxRate.toFixed(2), 2);
  if (amount <= 0n || (type === 1 && amount > salaryCents * 100n) || (type === 2 && amount > 1000000n)) return null;
  const gross = type === 1 ? roundedDivide(amount, 100n) : roundedDivide(salaryCents * amount, 1000000n);
  if (gross <= 0n) return null;
  const tax = roundedDivide(gross * taxHundredths, 10000n);
  return { gross: Number(gross) / 100, tax: Number(tax) / 100, net: Number(gross - tax) / 100 };
}

export function currentLimaMonth(): string {
  const parts = new Intl.DateTimeFormat('en', { timeZone: 'America/Lima', year: 'numeric', month: '2-digit' }).formatToParts(new Date());
  return `${parts.find(p => p.type === 'year')!.value}-${parts.find(p => p.type === 'month')!.value}`;
}
