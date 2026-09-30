import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { EditableArtistList } from './EditableArtistList';

const lineup = [{ name: 'Lunar Static' }, { name: 'Tiny Comet' }];

function setup() {
  const onChange = vi.fn();
  render(<EditableArtistList initialArtists={lineup} onChange={onChange} />);
  const lastNames = () => onChange.mock.lastCall?.[0].map((a: { name: string }) => a.name);
  return { onChange, lastNames, user: userEvent.setup() };
}

describe('EditableArtistList', () => {
  it('shows the lineup with a count', () => {
    setup();

    expect(screen.getByText('2 artists in lineup')).toBeInTheDocument();
    expect(screen.getByText('Lunar Static')).toBeInTheDocument();
  });

  it('adds a missing artist on Enter', async () => {
    const { user, lastNames } = setup();

    await user.type(screen.getByPlaceholderText('Add missing artist...'), 'Fennel{Enter}');

    expect(lastNames()).toEqual(['Lunar Static', 'Tiny Comet', 'Fennel']);
  });

  it('ignores an artist already on the lineup, whatever the case', async () => {
    const { user, onChange } = setup();

    await user.type(screen.getByPlaceholderText('Add missing artist...'), 'tiny comet{Enter}');

    expect(onChange).not.toHaveBeenCalled();
  });

  it('fixes a misspelled name in place', async () => {
    const { user, lastNames } = setup();

    await user.click(screen.getByText('Tiny Comet'));
    const input = screen.getByDisplayValue('Tiny Comet');
    await user.clear(input);
    await user.type(input, 'Tiny Comets{Enter}');

    expect(lastNames()).toEqual(['Lunar Static', 'Tiny Comets']);
  });

  it('removes an artist whose name is cleared', async () => {
    const { user, lastNames } = setup();

    await user.click(screen.getByText('Lunar Static'));
    await user.clear(screen.getByDisplayValue('Lunar Static'));
    await user.keyboard('{Enter}');

    expect(lastNames()).toEqual(['Tiny Comet']);
  });
});
