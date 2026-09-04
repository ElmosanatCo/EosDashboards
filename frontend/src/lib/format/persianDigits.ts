const persianDigits = "۰۱۲۳۴۵۶۷۸۹";
const numberFormatter = new Intl.NumberFormat("fa-IR");

export function toPersianDigits(value: number | string) {
  return String(value).replace(
    /[0-9]/g,
    (digit) => persianDigits[Number(digit)],
  );
}

export function formatPersianNumber(value: number) {
  return numberFormatter.format(value);
}
