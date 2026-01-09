import { useState } from 'react'

import './App.css'

function App() {
  const [count, setCount] = useState(0);

  return (
    <>
      <div className='app-background'>
        <div className='top-banner'> 
          <img className='player-image' src='public/operator.png'></img>
          <h1> Player Name</h1>
        </div>

        <div className='center-page'>
        <div className="stats-card">
          <h2>Kills: 120</h2>
        </div>
        <div className="stats-card">
          <h2>Deaths: 30</h2>
        </div>
        </div>
      </div>
    </>
  )
}

export default App
