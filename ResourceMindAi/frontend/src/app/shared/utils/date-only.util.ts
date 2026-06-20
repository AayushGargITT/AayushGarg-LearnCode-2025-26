export function toDateOnlyString(value: Date | string): string {
  if (value instanceof Date) {
    return formatDateParts(
      value.getFullYear(),
      value.getMonth() + 1,
      value.getDate()
    );
  }

  const dateOnlyMatch = /^(\d{4})-(\d{2})-(\d{2})/.exec(value);
  if (dateOnlyMatch) {
    return `${dateOnlyMatch[1]}-${dateOnlyMatch[2]}-${dateOnlyMatch[3]}`;
  }

  const parsed = new Date(value);
  return formatDateParts(
    parsed.getFullYear(),
    parsed.getMonth() + 1,
    parsed.getDate()
  );
}

export function formatDateOnly(value: Date | string): string {
  const [year, month, day] = toDateOnlyString(value).split('-');
  return `${day}-${month}-${year}`;
}

function formatDateParts(year: number, month: number, day: number): string {
  return [
    year,
    String(month).padStart(2, '0'),
    String(day).padStart(2, '0')
  ].join('-');
}
